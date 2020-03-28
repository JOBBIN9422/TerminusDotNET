using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using TerminusDotNetCore.Modules;
using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TerminusDotNetCore.Helpers;
using VideoLibrary;
using Google.Apis.YouTube.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using System.Threading;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using System.Text.RegularExpressions;

namespace TerminusDotNetCore.Services
{
    public class AudioService : ICustomService
    {
        //shared config object - passed from parent module via DI
        public IConfiguration Config { get; set; }
        //shared client secrets object - passed from parent module via DI
        public IConfiguration ClientSecrets { get; set; }

        //reference to the controlling module 
        public ServiceControlModule ParentModule { get; set; }

        //primary queue and backup (used when switching voice contexts)
        private ConcurrentQueue<AudioItem> _songQueue = new ConcurrentQueue<AudioItem>();
        private ConcurrentQueue<AudioItem> _backupQueue = new ConcurrentQueue<AudioItem>();

        //metadata about the currently playing song
        private AudioItem _currentSong = null;

        //the currently active ffmpeg process for audio streaming
        private Process _ffmpeg = null;

        //state flags
        private bool _playing = false;
        private bool _weedStarted = false;
        private bool _weedPlaying = false;

        private YouTubeService _ytService;

        //Discord info objects
        public IGuild Guild { get; set; }
        public DiscordSocketClient Client;

        public AudioService()
        {
            Task.Run(async () => await InitYoutubeService());
        }

        //path for local (aliased) audio files
        public string AudioPath { get; } = Path.Combine("assets", "audio");

        //map clients to their current channel
        private readonly ConcurrentDictionary<ulong, Tuple<IAudioClient, IVoiceChannel>> _connectedChannels = new ConcurrentDictionary<ulong, Tuple<IAudioClient, IVoiceChannel>>();

        public async Task InitYoutubeService()
        {
            UserCredential credentials;
            using (var credStream = new FileStream("youtube-secrets.json", FileMode.Open, FileAccess.Read))
            {
                credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(credStream).Secrets,
                    new[] { YouTubeService.Scope.Youtube },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(this.GetType().ToString())
                );
            }

            _ytService = new YouTubeService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credentials,
                    ApplicationName = this.GetType().ToString()
                }
            );
        }

        public async void SetGuildClient(IGuild g, DiscordSocketClient c)
        {
            Guild = g;
            Client = c;
            if (_weedStarted == false)
            {
                _weedStarted = true;
                ulong voiceID = ulong.Parse(Config["WeedChannelId"]);
                IVoiceChannel vc = await Guild.GetVoiceChannelAsync(voiceID);
                await this.ScheduleWeed(Guild, vc, Config["FfmpegCommand"]);
            }
        }

        public async Task JoinAudio(IGuild guild, IVoiceChannel target)
        {
            if (target.Guild.Id != guild.Id)
            {
                return;
            }

            await LeaveAudio(guild);
            await Task.Delay(100);
            var audioClient = await target.ConnectAsync();

            if (_connectedChannels.TryAdd(guild.Id, new Tuple<IAudioClient, IVoiceChannel>(audioClient, target)))
            {
                // If you add a method to log happenings from this service,
                // you can uncomment these commented lines to make use of that.
                //await Log(LogSeverity.Info, $"Connected to voice on {guild.Name}.");
            }
        }

        public async Task LeaveAudio(IGuild guild)
        {
            _currentSong = null;
            if (_ffmpeg != null && !_ffmpeg.HasExited)
            {
                _ffmpeg.Kill(true);
            }

            Tuple<IAudioClient, IVoiceChannel> client;
            if (_connectedChannels.TryRemove(guild.Id, out client))
            {
                await client.Item1.StopAsync();
                //await Log(LogSeverity.Info, $"Disconnected from voice on {guild.Name}.");
            }
        }

        public async Task SendAudioAsync(IGuild guild, string path, string command)
        {
            // Your task: Get a full path to the file if the value of 'path' is only a filename.
            Tuple<IAudioClient, IVoiceChannel> client;
            if (_connectedChannels.TryGetValue(guild.Id, out client))
            {
                //set playback state and spawn the stream process
                _playing = true;
                _ffmpeg = CreateProcess(path, command);

                //init audio stream in voice channel
                using (var stream = client.Item1.CreatePCMStream(AudioApplication.Music))
                {
                    try 
                    { 
                        //copy ffmpeg output to the voice channel stream
                        await _ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream); 
                    }
                    finally 
                    { 
                        //clean up ffmpeg, index queue, and set playback state
                        await stream.FlushAsync(); 
                        stream.Close(); 
                        _ffmpeg.Kill(true); 
                        _playing = false; 
                        await PlayNextInQueue(guild, command); 
                    }
                }
            }
        }

        public async Task QueueLocalSong(IGuild guild, string path, ulong channelId, string command)
        {
            string displayName = Path.GetFileNameWithoutExtension(path);
            if (_weedPlaying)
            {
                _backupQueue.Enqueue(new AudioItem() { Path = path, PlayChannelId = channelId, AudioSource = AudioType.Local, DisplayName = displayName });
            }
            else
            {
                _songQueue.Enqueue(new AudioItem() { Path = path, PlayChannelId = channelId, AudioSource = AudioType.Local, DisplayName = displayName });
                if (!_playing)
                {
                    //want to trigger playing next song in queue
                    await PlayNextInQueue(guild, command);
                }
            }
        }

        public async Task QueueYoutubePlaylist(IGuild guild, string playlistURL, ulong channelId, string command)
        {
            //check if the given URL refers to a youtube playlist
            if (!PlaylistUrlIsValid(playlistURL))
            {
                await ParentModule.ServiceReplyAsync("The given URL is not a valid YouTube playlist.");
                return;
            }

            List<string> videoUrls = new List<string>();
            string nextPageToken = "";

            //iterate over paginated playlist results from youtube and extract video URLs
            while (nextPageToken != null)
            {
                //prepare a paged playlist request for the given playlist URL
                var playlistRequest = _ytService.PlaylistItems.List("snippet,contentDetails");
                playlistRequest.PlaylistId = GetPlaylistIdFromUrl(playlistURL);
                playlistRequest.MaxResults = 50;
                playlistRequest.PageToken = nextPageToken;

                var searchListResponse = await playlistRequest.ExecuteAsync();
                
                //iterate over the results and build each video URL
                foreach (var item in searchListResponse.Items)
                {
                    string videoUrl = $"http://www.youtube.com/watch?v={item.Snippet.ResourceId.VideoId}";
                    videoUrls.Add(videoUrl);
                }

                //index to the next page of results
                nextPageToken = searchListResponse.NextPageToken;
            }

            //add the list of URLs to the queue for downloading during playback
            await QueueYoutubeURLs(videoUrls, guild, channelId, command);
        }

        private static bool PlaylistUrlIsValid(string url)
        {
            return Regex.IsMatch(url, @"https:\/\/www.youtube.com\/playlist\?list=.+");
        }

        private static string GetPlaylistIdFromUrl(string url)
        {
            string Id = Regex.Match(url, "list=.+").Value.Replace("list=", string.Empty);
            return Id;
        }

        private static string GetVideoIdFromUrl(string url)
        {
            string Id = Regex.Match(url, "v=.[A-Za-z0-9-_]+").Value.Replace("v=", string.Empty);
            return Id;
        }

        private async Task<string> GetVideoTitleFromUrlAsync(string url)
        {
            var videoRequest = _ytService.Videos.List("snippet");
            videoRequest.Id = GetVideoIdFromUrl(url);

            var searchResponse = await videoRequest.ExecuteAsync();

            string title = string.Empty;

            //grab the video title from the response (any should do)
            foreach (var item in searchResponse.Items)
            {
                if (!string.IsNullOrEmpty(item.Snippet.Title))
                {
                    title = item.Snippet.Title;
                    break;
                }
            }

            return title;
        }

        public async Task QueueSearchedYoutubeSong(IGuild guild, string searchTerm, ulong channelId, string command)
        {
            var searchListRequest = _ytService.Search.List("snippet");
            searchListRequest.Q = searchTerm;
            searchListRequest.MaxResults = 10;

            //run the search with the given term and fetch resulting video URLs
            var searchListResponse = await searchListRequest.ExecuteAsync();

            foreach (var searchResult in searchListResponse.Items)
            {
                string url = $"http://www.youtube.com/watch?v={searchResult.Id.VideoId}";

                try
                {
                    await QueueYoutubeSongPreDownloaded(guild, url, channelId, command);

                    //if we successfully download and queue a song, exit this loop and return
                    return;
                }
                catch (ArgumentException)
                {
                    //try to download the next song in the list
                    continue;
                }
            }

            await ParentModule.ServiceReplyAsync($"No videos were successfully downloaded for the search term '{searchTerm}'.");
        }

        private async Task QueueYoutubeURLs(List<string> urls, IGuild guild, ulong channelId, string command)
        {
            //enqueue all of the URLs before starting playback 
            foreach (string url in urls)
            {
                string displayName = await GetVideoTitleFromUrlAsync(url);
                AudioItem currVideo = new AudioItem()
                {
                    Path = url,
                    PlayChannelId = channelId,
                    AudioSource = AudioType.YoutubeUrl,
                    DisplayName = displayName
                };

                if (_weedPlaying)
                {
                    _backupQueue.Enqueue(currVideo);
                }
                else
                {
                    _songQueue.Enqueue(currVideo);
                }
            }

            if (!_playing)
            {
                //want to trigger playing next song in queue
                await PlayNextInQueue(guild, command);
            }
        }

        public async Task QueueYoutubeSongPreDownloaded(IGuild guild, string path, ulong channelId, string command)
        {
            //set metadata for the current audio item
            AudioType audioType = AudioType.YoutubeDownloaded;
            string displayName = await GetVideoTitleFromUrlAsync(path);

            //get a local file for the current video
            path = await DownloadYoutubeVideoAsync(path);

            //queue the audio item
            if (_weedPlaying)
            {
                _backupQueue.Enqueue(new AudioItem() { Path = path, PlayChannelId = channelId, AudioSource = audioType, DisplayName = displayName });
            }
            else
            {
                _songQueue.Enqueue(new AudioItem() { Path = path, PlayChannelId = channelId, AudioSource = audioType, DisplayName = displayName });

                if (!_playing)
                {
                    //want to trigger playing next song in queue
                    await PlayNextInQueue(guild, command);
                }
            }
        }

        public async Task QueueTempSong(IGuild guild, IReadOnlyCollection<Attachment> attachments, ulong channelId, string command)
        {
            List<string> files = AttachmentHelper.DownloadAttachments(attachments);
            string path = files[0];
            string displayName = Path.GetFileName(path);
            if (_weedPlaying)
            {
                _backupQueue.Enqueue(new AudioItem() { Path = path, PlayChannelId = channelId, AudioSource = AudioType.Attachment, DisplayName = displayName });
            }
            else
            {
                _songQueue.Enqueue(new AudioItem() { Path = path, PlayChannelId = channelId, AudioSource = AudioType.Attachment, DisplayName = displayName });
                if (!_playing)
                {
                    //want to trigger playing next song in queue
                    await PlayNextInQueue(guild, command);
                }
            }
        }

        public async Task PlayNextInQueue(IGuild guild, string command)
        {
            AudioItem nextInQueue;
            if (_songQueue.TryDequeue(out nextInQueue))
            {
                //need to download if not already saved locally (change the URL to the path of the downloaded file)
                if (nextInQueue.AudioSource == AudioType.YoutubeUrl)
                {
                    try
                    {
                        Console.WriteLine($"downloading local file for {nextInQueue.Path}...");
                        nextInQueue.Path = await DownloadYoutubeVideoAsync(nextInQueue.Path);
                        Console.WriteLine($"downloaded local file {nextInQueue.Path}");
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine($"failed to download local file for {nextInQueue.Path}, skipping...");
                        //skip this item if the download fails
                        await PlayNextInQueue(guild, command);
                        return;
                    }
                }

                IVoiceChannel channel = await guild.GetVoiceChannelAsync(nextInQueue.PlayChannelId);
                await JoinAudio(guild, channel);

                if (Client != null)
                {
                    await Client.SetGameAsync(Path.GetFileName(nextInQueue.Path));
                }

                //update the currently-playing song and kill the audio process if it's running
                _currentSong = nextInQueue;
                if (_ffmpeg != null && !_ffmpeg.HasExited)
                {
                    _ffmpeg.Kill(true);
                }

                await SendAudioAsync(guild, nextInQueue.Path, command);
            }
            else
            {
                await LeaveAudio(guild);
                if (Client != null)
                {
                    await Client.SetGameAsync(null);
                }
                // Queue is empty, delete all .mp3 files in the assets/temp folder
                CleanAudioFiles();

            }
        }

        public async Task StopAllAudio(IGuild guild)
        {
            _songQueue = new ConcurrentQueue<AudioItem>();
            _playing = false;
            _currentSong = null;
            await LeaveAudio(guild);
            CleanAudioFiles();

            if (Client != null)
            {
                await Client.SetGameAsync(null);
            }
            //probably should do this, but we would have to figure out a way to wait til the ffmpeg process dies, which I don't want to do
            //the files will get wiped out eventually I bet
            //AttachmentHelper.DeleteFiles(AttachmentHelper.GetTempAssets("*.mp3"));
        }

        public async Task PlayRegexAudio(IGuild guild, string filename)
        {
            ulong voiceID = ulong.Parse(Config["AudioChannelId"]);
            IVoiceChannel vc = await guild.GetVoiceChannelAsync(voiceID);
            _backupQueue = _songQueue;
            _weedPlaying = true;
            await StopAllAudio(guild);
            await JoinAudio(guild, vc);
            string path = Path.Combine(AudioPath, filename);
            path = Path.GetFullPath(path);
            await SendAudioAsync(guild, path, Config["FfmpegCommand"]);
            await LeaveAudio(guild);
            _weedPlaying = false;
            _songQueue = _backupQueue;
            _backupQueue = new ConcurrentQueue<AudioItem>();
            if (Client != null)
            {
                await Client.SetGameAsync(null);
            }
            await PlayNextInQueue(guild, Config["FfmpegCommand"]);
        }

        public void SaveSong(string alias, IReadOnlyCollection<Attachment> attachments)
        {
            string filename = AttachmentHelper.DownloadPersistentAudioAttachment(attachments.ElementAt(0));
            File.AppendAllText(Path.Combine(AudioPath, "audioaliases.txt"), alias + " " + filename + Environment.NewLine);
        }

        private Process CreateProcess(string path, string command)
        {
            //start an ffmpeg process with stdout redirected 
            return Process.Start(new ProcessStartInfo
            {
                FileName = command,
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
        }

        public async Task ScheduleWeed(IGuild guild, IVoiceChannel channel, string command)
        {
            DateTime now = DateTime.Now;
            DateTime fourTwenty = DateTime.Today.AddHours(16.333);
            if (now > fourTwenty)
            {
                fourTwenty = fourTwenty.AddDays(1.0);
            }
            await Task.Delay((int)fourTwenty.Subtract(DateTime.Now).TotalMilliseconds);
            _backupQueue = _songQueue;
            _weedPlaying = true;
            await StopAllAudio(guild);
            await JoinAudio(guild, channel);
            string path = Path.Combine(AudioPath, "weedlmao.mp3");
            path = Path.GetFullPath(path);
            if (Client != null)
            {
                await Client.SetGameAsync("weeeeed");
            }
            await SendAudioAsync(guild, path, command);
            await LeaveAudio(guild);
            _weedPlaying = false;
            _songQueue = _backupQueue;
            _backupQueue = new ConcurrentQueue<AudioItem>();
            if (Client != null)
            {
                await Client.SetGameAsync(null);
            }
            _ = PlayNextInQueue(guild, command);
            _ = ScheduleWeed(guild, channel, command);
        }

        public List<Embed> ListQueueContents()
        {
            //need a list of embeds since each embed can only have 25 fields max
            List<Embed> songList = new List<Embed>();
            int numSongs = _songQueue.Count;
            int entryCount = 0;

            //count the currently-playing song if any (it's not in the queue but needs to be listed)
            if (_currentSong != null)
            {
                numSongs++;
            }

            var embed = new EmbedBuilder
            {
                Title = $"{numSongs} Songs"
            };

            //add the currently-playing song to the list, if any
            if (_currentSong != null)
            {
                string songSource = GetAudioSourceString(_currentSong.AudioSource);
                embed.AddField($"{entryCount + 1}: {_currentSong.DisplayName} **(currently playing)**", songSource);
            }

            foreach (var songItem in _songQueue)
            {
                entryCount++;

                //if we have 25 entries in an embed already, need to make a new one 
                if (entryCount % EmbedBuilder.MaxFieldCount == 0 && entryCount > 0)
                {
                    songList.Add(embed.Build());
                    embed = new EmbedBuilder();
                }

                //add the current queue item to the song list 
                string songName = $"**{entryCount + 1}:** {songItem.DisplayName}";
                string songSource = GetAudioSourceString(songItem.AudioSource);

                embed.AddField(songName, songSource);
            }

            //add the most recently built embed if it's not in the list yet 
            if (songList.Count == 0 || !songList.Contains(embed.Build()))
            {
                songList.Add(embed.Build());
            }

            return songList;
        }

        public List<Embed> ListAvailableAliases()
        {
            //need a list of embeds since each embed can only have 25 fields max
            List<Embed> songList = new List<Embed>();
            string[] lines = File.ReadAllLines(Path.Combine(AudioPath, "audioaliases.txt"));
            int numEntries = 0;
            List<string[]> aliases = new List<string[]>();
            foreach (string line in lines)
            {
                if (line.StartsWith("#") || String.IsNullOrEmpty(line))
                {
                    continue;
                }
                string[] tmp = line.Split(" ");
                numEntries++;
                aliases.Add(tmp);
            }

            int entryCount = 0;

            var embed = new EmbedBuilder
            {
                Title = $"{numEntries} Songs Available"
            };

            //add the currently-playing song to the list, if any
            foreach (string[] alias in aliases)
            {
                entryCount++;

                //if we have 25 entries in an embed already, need to make a new one 
                if (entryCount % EmbedBuilder.MaxFieldCount == 0 && entryCount > 0)
                {
                    songList.Add(embed.Build());
                    embed = new EmbedBuilder();
                }

                //add the current queue item to the song list 
                string songName = alias[0];
                string songSource = alias[1];

                embed.AddField(songName, songSource);
            }

            //add the most recently built embed if it's not in the list yet 
            if (songList.Count == 0 || !songList.Contains(embed.Build()))
            {
                songList.Add(embed.Build());
            }

            return songList;
        }

        private string GetAudioSourceString(AudioType audioType)
        {
            string songSource = string.Empty;
            switch (audioType)
            {
                case AudioType.Local:
                    songSource = "Aliased audio file";
                    break;
                case AudioType.YoutubeDownloaded:
                    songSource = "YouTube (pre-downloaded)";
                    break;
                case AudioType.YoutubeUrl:
                    songSource = "YouTube (queued URL)";
                    break;
                case AudioType.Attachment:
                    songSource = "User-attached file";
                    break;
                default:
                    songSource = "Unknown source";
                    break;
            }
            return songSource;
        }

        private async Task<string> DownloadYoutubeVideoAsync(string url)
        {
            //define the directory to save video files to
            string tempPath = Path.Combine(Environment.CurrentDirectory, "assets", "temp");
            string videoDataFilename;

            try
            {
                //download the youtube video data (usually .mp4 or .webm)
                var youtube = YouTube.Default;
                var video = await youtube.GetVideoAsync(url);
                var videoData = await video.GetBytesAsync();

                //write the downloaded media file to the temp assets dir
                videoDataFilename = Path.Combine(tempPath, video.FullName);
                await File.WriteAllBytesAsync(videoDataFilename, videoData);
                
                return videoDataFilename;
            }
            catch (Exception ex)
            {
                //give a more helpful error message
                throw new ArgumentException($"Could not download a video file for the given URL: '{ex.Message}'.", ex);
            }
        }

        private void CleanAudioFiles()
        {
            AttachmentHelper.DeleteFiles(AttachmentHelper.GetTempAssets("*.mp3"));
            AttachmentHelper.DeleteFiles(AttachmentHelper.GetTempAssets("*.mp4"));
            AttachmentHelper.DeleteFiles(AttachmentHelper.GetTempAssets("*.webm"));
        }

    }
}
