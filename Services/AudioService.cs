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
using Newtonsoft.Json;

namespace TerminusDotNetCore.Services
{
    public class AudioService : ICustomService
    {
        //shared config object - passed from parent module via DI
        public IConfiguration Config { get; set; }

        //reference to the controlling module 
        public ServiceControlModule ParentModule { get; set; }

        //primary queue and backup (used when switching voice contexts)
        private ConcurrentQueue<AudioItem> _songQueue = new ConcurrentQueue<AudioItem>();
        private ConcurrentQueue<AudioItem> _backupQueue = new ConcurrentQueue<AudioItem>();

        //metadata about the currently playing song
        private AudioItem _currentSong = null;

        public IVoiceChannel CurrentChannel { get; set; } = null;

        //the currently active ffmpeg process for audio streaming
        private Process _ffmpeg = null;

        private readonly string FFMPEG_PROCESS_NAME;

        //JSON converter settings for queue save/load
        private static readonly JsonSerializerSettings JSON_SETTINGS = new JsonSerializerSettings()
        {
            Formatting = Formatting.None,
            TypeNameHandling = TypeNameHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        //state flags
        private bool _playing = false;
        private bool _weedStarted = false;
        private bool _weedPlaying = false;

        private YouTubeService _ytService;

        //Discord info objects
        public IGuild Guild { get; set; }
        public DiscordSocketClient Client;

        public AudioService(IConfiguration config)
        {
            Config = config;
            FFMPEG_PROCESS_NAME = Config["FfmpegCommand"];
            Task.Run(async () => await InitYoutubeService());
        }

        //path for local (aliased) audio files
        public string AudioPath { get; } = Path.Combine("assets", "audio");

        //map clients to their current channel
        private readonly ConcurrentDictionary<ulong, Tuple<IAudioClient, IVoiceChannel>> _connectedChannels = new ConcurrentDictionary<ulong, Tuple<IAudioClient, IVoiceChannel>>();

        public async Task InitYoutubeService()
        {
            //attempt to read auth info from secrets file 
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
                await this.ScheduleWeed();
            }
        }

        public async Task JoinAudio(int retryCount = 5)
        {
            await LeaveAudio();

            IAudioClient audioClient = null;
            try
            {
                audioClient = await CurrentChannel.ConnectAsync();
            }
            catch (TimeoutException)
            {
                if (retryCount == 0)
                {
                    await Bot.Log(new LogMessage(LogSeverity.Error, "AudioSvc", $"failed to connect to voice channel after repeated timeouts."));
                    return;
                }

                await Bot.Log(new LogMessage(LogSeverity.Error, "AudioSvc", $"failed to connect to voice channel, retrying... ({retryCount} attempts remaining)"));
                await JoinAudio(--retryCount);
                return;
            }

            if (_connectedChannels.TryAdd(Guild.Id, new Tuple<IAudioClient, IVoiceChannel>(audioClient, CurrentChannel)))
            {
                // If you add a method to log happenings from this service,
                // you can uncomment these commented lines to make use of that.
                //await Log(LogSeverity.Info, $"Connected to voice on {guild.Name}.");
            }
        }

        public async Task LeaveAudio()
        {
            _currentSong = null;
            _playing = false;
            if (_ffmpeg != null && !_ffmpeg.HasExited)
            {
                _ffmpeg.Kill(true);
            }

            if (CurrentChannel != null)
            {
                await CurrentChannel.DisconnectAsync();
            }

            Tuple<IAudioClient, IVoiceChannel> client;
            if (_connectedChannels.TryRemove(Guild.Id, out client))
            {
                await client.Item1.StopAsync();
                //await Log(LogSeverity.Info, $"Disconnected from voice on {guild.Name}.");
            }
        }

        public async Task SendAudioAsync(string path)
        {
            Tuple<IAudioClient, IVoiceChannel> client;
            if (_connectedChannels.TryGetValue(Guild.Id, out client))
            {
                //clean up the existing process if necessary
                if (_ffmpeg != null && !_ffmpeg.HasExited)
                {
                    _ffmpeg.Kill(true);
                }

                //set playback state and spawn the stream process
                _playing = true;
                _ffmpeg = CreateProcess(path);

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

                        _ffmpeg.Kill(true);
                        _playing = false;
                        //await PlayNextInQueue(guild);
                    }
                }
            }
        }

        private void EnqueueSong(AudioItem item)
        {
            if (_weedPlaying)
            {
                _backupQueue.Enqueue(item);
            }
            else
            {
                _songQueue.Enqueue(item);
            }
        }

        public async Task QueueLocalSong(SocketUser owner, string path, ulong channelId)
        {
            string displayName = Path.GetFileNameWithoutExtension(path);
            EnqueueSong(new LocalAudioItem() { Path = path, PlayChannelId = channelId, AudioSource = FileAudioType.Local, DisplayName = displayName, OwnerName = owner.Username });

            if (!_playing)
            {
                //want to trigger playing next song in queue
                await PlayNextInQueue();
            }
            else
            {
                await SaveQueueContents();
            }
        }

        public async Task QueueYoutubePlaylist(SocketUser owner, string playlistURL, ulong channelId)
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
            await QueueYoutubeURLs(videoUrls, owner, channelId);
        }

        private static bool PlaylistUrlIsValid(string url)
        {
            return Regex.IsMatch(url, @"https:\/\/www.youtube.com\/playlist\?list=.+");
        }

        private static string GetPlaylistIdFromUrl(string url)
        {
            string Id = Regex.Match(url, @"(?<=list=).+").Value;
            return Id;
        }

        private static string GetVideoIdFromUrl(string url)
        {
            string Id = Regex.Match(url, @"(?<=v=)[\w-]+").Value;
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

        public async Task QueueSearchedYoutubeSong(SocketUser owner, string searchTerm, ulong channelId)
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
                    await QueueYoutubeSongPreDownloaded(owner, url, channelId);

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

        private async Task QueueYoutubeURLs(List<string> urls, SocketUser owner, ulong channelId)
        {
            //enqueue all of the URLs before starting playback 
            foreach (string url in urls)
            {
                string displayName = await GetVideoTitleFromUrlAsync(url);

                //create the current video item (file path cannot be set until it is downloaded when dequeued)
                YouTubeAudioItem currVideo = new YouTubeAudioItem()
                {
                    VideoUrl = url,
                    PlayChannelId = channelId,
                    AudioSource = YouTubeAudioType.Url,
                    DisplayName = displayName,
                    OwnerName = owner.Username
                };

                EnqueueSong(currVideo);
            }

            if (!_playing)
            {
                //want to trigger playing next song in queue
                await PlayNextInQueue();
            }
            else
            {
                await SaveQueueContents();
            }
        }

        public async Task QueueYoutubeSongPreDownloaded(SocketUser owner, string url, ulong channelId)
        {
            string displayName = await GetVideoTitleFromUrlAsync(url);

            //get a local file for the current video
            string filePath = await DownloadYoutubeVideoAsync(url);

            EnqueueSong(new YouTubeAudioItem() { Path = filePath, VideoUrl = url, PlayChannelId = channelId, AudioSource = YouTubeAudioType.PreDownloaded, DisplayName = displayName, OwnerName = owner.Username });
            
            if (!_playing)
            {
                await PlayNextInQueue();
            }
            else
            {
                await SaveQueueContents();
            }
        }

        public async Task QueueTempSong(SocketUser owner, IReadOnlyCollection<Attachment> attachments, ulong channelId)
        {
            List<string> files = AttachmentHelper.DownloadAttachments(attachments);
            string path = files[0];
            string displayName = Path.GetFileName(path);

            EnqueueSong(new LocalAudioItem() { Path = path, PlayChannelId = channelId, AudioSource = FileAudioType.Attachment, DisplayName = displayName, OwnerName = owner.Username });

            if (!_playing)
            {
                //want to trigger playing next song in queue
                await PlayNextInQueue();
            }
            else
            {
                await SaveQueueContents();
            }
        }

        public async Task PlayNextInQueue()
        {
            AudioItem nextInQueue;
            if (_songQueue.TryDequeue(out nextInQueue))
            {
                //need to download if not already saved locally (change the URL to the path of the downloaded file)
                if (nextInQueue is YouTubeAudioItem)
                {
                    YouTubeAudioItem nextVideo = nextInQueue as YouTubeAudioItem;
                    try
                    {
                        //if the youtube video has not been downloaded yet
                        if (nextVideo.AudioSource == YouTubeAudioType.Url)
                        {
                            await Bot.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"downloading local file for {nextVideo.DisplayName}..."));

                            nextVideo.Path = await DownloadYoutubeVideoAsync(nextVideo.VideoUrl);

                            await Bot.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"downloaded local file {nextVideo.Path}"));
                        }
                    }
                    catch (ArgumentException)
                    {
                        await Bot.Log(new LogMessage(LogSeverity.Warning, "AudioSvc", $"failed to download local file for {nextVideo.DisplayName}, skipping..."));

                        //skip this item if the download fails
                        await PlayNextInQueue();
                        return;
                    }
                }

                CurrentChannel = await Guild.GetVoiceChannelAsync(nextInQueue.PlayChannelId);
                await JoinAudio();

                if (Client != null)
                {
                    await Client.SetGameAsync(nextInQueue.DisplayName);
                }

                //update the currently-playing song and kill the audio process if it's running
                _currentSong = nextInQueue;

                //record current queue state 
                await SaveQueueContents();

                //play audio on channel
                await SendAudioAsync(nextInQueue.Path);

                //play next in queue (if any)
                await PlayNextInQueue();
            }
            else
            {
                await LeaveAudio();
                if (Client != null)
                {
                    await Client.SetGameAsync(null);
                }
                // Queue is empty, delete all .mp3 files in the assets/temp folder
                CleanAudioFiles();

            }
        }

        public async Task LoadQueueContents()
        {
            string queueFilename = Path.Combine(AudioPath, "backup", "queue-contents.json");
            if (!File.Exists(queueFilename))
            {
                throw new FileNotFoundException("No queue backup file was found in the backup directory.");
            }
            using (StreamReader jsonReader = new StreamReader(queueFilename))
            {
                //read queue file contents into array of lines
                string[] text = (await jsonReader.ReadToEndAsync()).Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                //reset the queue
                _songQueue.Clear();

                if (_playing)
                {
                    await LeaveAudio();
                }

                //deserialize and enqueue each saved item
                foreach (string currLine in text)
                {
                    if (!string.IsNullOrEmpty(currLine))
                    {
                        AudioItem currItem = JsonConvert.DeserializeObject<AudioItem>(currLine, JSON_SETTINGS);
                        EnqueueSong(currItem);
                    }
                }
            }

            if (!_playing)
            {
                await PlayNextInQueue();
            }
        }

        public async Task SaveQueueContents()
        {
            //store the queue contents to file
            using (StreamWriter jsonWriter = new StreamWriter(Path.Combine(AudioPath, "backup", "queue-contents.json"), false))
            {
                //save the current song (if any)
                if (_currentSong != null)
                {
                    await SaveSongToFile(jsonWriter, _currentSong);
                }

                //save each item currently in the queue
                foreach (AudioItem item in _songQueue)
                {
                    await SaveSongToFile(jsonWriter, item);
                }
            }
        }

        private async Task SaveSongToFile(StreamWriter writer, AudioItem song)
        {
            if (song is YouTubeAudioItem)
            {
                YouTubeAudioItem ytSong = song as YouTubeAudioItem;

                if (!string.IsNullOrEmpty(ytSong.VideoUrl))
                {
                    //don't want to alter the current item, so create a "copy" and save it to file
                    YouTubeAudioItem saveItem = new YouTubeAudioItem()
                    {
                        //force source to youtube URL to force redownload
                        AudioSource = YouTubeAudioType.Url,
                        VideoUrl = ytSong.VideoUrl,
                        DisplayName = ytSong.DisplayName,
                        OwnerName = ytSong.OwnerName,
                        PlayChannelId = ytSong.PlayChannelId,

                        //set path to empty - temp file may not exist when the queue is re-loaded
                        Path = string.Empty
                    };

                    await writer.WriteLineAsync(JsonConvert.SerializeObject(saveItem, JSON_SETTINGS));
                }
            }
            else if (song is LocalAudioItem)
            {
                LocalAudioItem localSong = song as LocalAudioItem;
                if (localSong.AudioSource == FileAudioType.Attachment)
                {
                    //define the destination filename for the attachment in the backup dir
                    string attachFilename = Path.GetFileName(localSong.Path);
                    string backupPath = Path.Combine(AudioPath, "backup");

                    //copy the attached file to the backup dir if necessary
                    string backupFilename = Path.Combine(backupPath, attachFilename);
                    if (!File.Exists(backupFilename))
                    {
                        File.Copy(localSong.Path, backupFilename);
                    }

                    //save the current item to the file
                    LocalAudioItem saveItem = new LocalAudioItem()
                    {
                        AudioSource = localSong.AudioSource,
                        Path = backupFilename,
                        DisplayName = localSong.DisplayName,
                        OwnerName = localSong.OwnerName,
                        PlayChannelId = localSong.PlayChannelId
                    };

                    await writer.WriteLineAsync(JsonConvert.SerializeObject(saveItem, JSON_SETTINGS));
                }
                else
                {
                    //no need to move any files if it's a persistent audio item
                    await writer.WriteLineAsync(JsonConvert.SerializeObject(localSong, JSON_SETTINGS));
                }
            }
            else
            {
                return;
            }

        }

        public async Task StopAllAudio()
        {
            _songQueue = new ConcurrentQueue<AudioItem>();
            _playing = false;
            _currentSong = null;
            await LeaveAudio();
            CleanAudioFiles();

            if (Client != null)
            {
                await Client.SetGameAsync(null);
            }
        }

        public async Task PlayRegexAudio(string filename)
        {
            ulong voiceID = ulong.Parse(Config["AudioChannelId"]);
            IVoiceChannel vc = await Guild.GetVoiceChannelAsync(voiceID);

            _backupQueue = _songQueue;
            _weedPlaying = true;

            await StopAllAudio();
            await JoinAudio();

            string path = Path.Combine(AudioPath, filename);
            path = Path.GetFullPath(path);

            await SendAudioAsync(path);
            await LeaveAudio();

            _weedPlaying = false;
            _songQueue = _backupQueue;
            _backupQueue = new ConcurrentQueue<AudioItem>();

            if (Client != null)
            {
                await Client.SetGameAsync(null);
            }

            await PlayNextInQueue();
        }

        public void SaveSong(string alias, IReadOnlyCollection<Attachment> attachments)
        {
            string filename = AttachmentHelper.DownloadPersistentAudioAttachment(attachments.ElementAt(0));
            File.AppendAllText(Path.Combine(AudioPath, "audioaliases.txt"), alias + " " + filename + Environment.NewLine);
        }

        public async Task SaveCurrentSong(string alias)
        {
            if (_currentSong == null)
            {
                await ParentModule.ServiceReplyAsync("No song is currently playing.");
                return;
            }
            if (string.IsNullOrEmpty(alias))
            {
                await ParentModule.ServiceReplyAsync("Please provide a name to alias the song by.");
                return;
            }

            //move the temp file to the alias directory 
            string newFileName = $"{alias}{Path.GetExtension(_currentSong.Path)}";
            string newPath = Path.Combine(AudioPath, newFileName);
            File.Copy(_currentSong.Path, newPath);

            //add the song to the alias file
            File.AppendAllText(Path.Combine(AudioPath, "audioaliases.txt"), alias + " " + newFileName + Environment.NewLine);

            await ParentModule.ServiceReplyAsync($"Successfully added aliased song '{alias}'.");
        }

        private Process CreateProcess(string path)
        {
            //start an ffmpeg process with stdout redirected 
            return Process.Start(new ProcessStartInfo
            {
                FileName = FFMPEG_PROCESS_NAME,
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
        }

        public async Task ScheduleWeed()
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

            await StopAllAudio();
            await JoinAudio();

            string path = Path.Combine(AudioPath, "weedlmao.mp3");
            path = Path.GetFullPath(path);

            if (Client != null)
            {
                await Client.SetGameAsync("weeeeed");
            }

            await SendAudioAsync(path);
            await LeaveAudio();

            _weedPlaying = false;
            _songQueue = _backupQueue;
            _backupQueue = new ConcurrentQueue<AudioItem>();

            if (Client != null)
            {
                await Client.SetGameAsync(null);
            }
            _ = PlayNextInQueue();
            _ = ScheduleWeed();
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
                string songSource = GetAudioSourceString(_currentSong);
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
                string songSource = GetAudioSourceString(songItem);

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

        private string GetAudioSourceString(AudioItem audioItem)
        {
            string songSource = string.Empty;
            if (audioItem is YouTubeAudioItem)
            {
                YouTubeAudioItem ytAudioItem = audioItem as YouTubeAudioItem;
                switch (ytAudioItem.AudioSource)
                {
                    case YouTubeAudioType.PreDownloaded:
                        songSource = $"[pre-downloaded YouTube audio]({ytAudioItem.VideoUrl})";
                        break;
                    case YouTubeAudioType.Url:
                        songSource = $"[queued YouTube download]({ytAudioItem.VideoUrl})";
                        break;
                }
            }
            else if (audioItem is LocalAudioItem)
            {
                LocalAudioItem localAudioItem = audioItem as LocalAudioItem;
                switch (localAudioItem.AudioSource)
                {
                    case FileAudioType.Attachment:
                        songSource = "User-attached file";
                        break;
                    case FileAudioType.Local:
                        songSource = "Aliased audio file";
                        break;
                }
            }
            else
            {
                songSource = "Unknown source";
            }

            if (!string.IsNullOrEmpty(audioItem.OwnerName))
            {
                songSource = $"{songSource} // added by {audioItem.OwnerName}";
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

                //remove single/double quotes for command line parsing purposes
                string vidNameEscapedQuotes = video.FullName.Replace("\"", "").Replace("'", "");

                //write the downloaded media file to the temp assets dir
                videoDataFilename = Path.Combine(tempPath, vidNameEscapedQuotes);
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
