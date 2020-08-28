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
using Discord.Commands;

namespace TerminusDotNetCore.Services
{
    public class AudioService : ICustomService
    {
        #region fields & properties
        //shared config object - passed from parent module via DI
        public IConfiguration Config { get; set; }

        //reference to the controlling module 
        public ServiceControlModule ParentModule { get; set; }

        //primary queue and backup (used when switching voice contexts)
        private LinkedList<AudioItem> _songQueue = new LinkedList<AudioItem>();

        //lock object for queue synchronization
        private readonly object _queueLock = new object();

        //cache hideki playlist songs to prevent too many API calls
        private List<string> _hidekiSongsCache = new List<string>();

        private DateTime _lastHidekiReload = DateTime.MinValue;

        //metadata about the currently playing song
        private AudioItem _currentSong = null;

        private Task _currentAudioStreamTask = null;

        //currently-connected channel (needs to be set from outside the service occasionally)
        public IVoiceChannel CurrentChannel { get; set; } = null;

        //used for streaming audio in the currently-connected channel
        private IAudioClient _currAudioClient = null;

        //the currently active ffmpeg process for audio streaming
        private CancellationTokenSource _ffmpegCancelTokenSrc = new CancellationTokenSource();

        //command name (.exe extension for Windows use)
        private static string FFMPEG_PROCESS_NAME;

        //JSON converter settings for queue save/load
        private static readonly JsonSerializerSettings JSON_SETTINGS = new JsonSerializerSettings()
        {
            Formatting = Formatting.None,
            TypeNameHandling = TypeNameHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        //state flags
        private bool _playing = false;
        private bool _weedPlaying = false;

        private YouTubeService _ytService;

        //Discord info objects
        public IGuild Guild { get; set; }
        public DiscordSocketClient Client { get; set; }

        //path for local (aliased) audio files
        public string AudioPath { get; } = Path.Combine("assets", "audio");

        //RNG
        private Random _random;
        #endregion

        #region init
        public AudioService(IConfiguration config, Random random)
        {
            _random = random;
            Config = config;
            FFMPEG_PROCESS_NAME = Config["FfmpegCommand"];
            Task.Run(async () => await InitYoutubeService());
        }

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

            await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", "Initialized youtube service."));
        }
        #endregion

        #region audio control methods
        public async Task StopAllAudio()
        {
            await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", "Stopping all audio..."));

            _songQueue = new LinkedList<AudioItem>();
            _playing = false;
            _currentSong = null;
            await LeaveAudio();
            CleanAudioFiles();

            if (Client != null)
            {
                await Client.SetGameAsync(null);
            }
            await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", "Stopped audio and deleted temp audio files."));
        }

        public void StopFfmpeg()
        {
            //stop any currently active streams
            if (_ffmpegCancelTokenSrc != null)
            {
                lock (_ffmpegCancelTokenSrc)
                {
                    //cancel and re-init the cancellation source
                    _ffmpegCancelTokenSrc.Cancel();
                }
            }
        }

        public async Task JoinAudio(int retryCount = 3)
        {
            try
            {
                _currAudioClient = await CurrentChannel.ConnectAsync();
                await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Joined audio on channel '{CurrentChannel.Name}'."));
            }

            //attempt to rejoin on timeout
            catch (TimeoutException)
            {
                if (retryCount == 0)
                {
                    await Logger.Log(new LogMessage(LogSeverity.Error, "AudioSvc", $"failed to connect to voice channel after repeated timeouts."));
                    return;
                }

                await Logger.Log(new LogMessage(LogSeverity.Error, "AudioSvc", $"failed to connect to voice channel, retrying... ({retryCount} attempts remaining)"));
                await JoinAudio(--retryCount);
                return;
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }

        public async Task LeaveAudio()
        {
            //update playing & song states
            _currentSong = null;
            _playing = false;
            if (Client != null)
            {
                await Client.SetGameAsync("");
            }


            //disconnect and stop the audio client if needed
            if (CurrentChannel != null)
            {
                await CurrentChannel.DisconnectAsync();
                await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Disconnected from channel."));
                CurrentChannel = null;
            }
            if (_currAudioClient != null)
            {
                await _currAudioClient.StopAsync();
                _currAudioClient = null;
            }
        }

        public async Task SendAudioAsync(string path)
        {
            if (_currAudioClient != null)
            {
                //set playback state and spawn the stream process
                _playing = true;
                await StreamFfmpegAudio(path);
            }
        }

        private async Task StreamFfmpegAudio(string path)
        {
            //init ffmpeg and audio streams
            using (var ffmpeg = CreateProcess(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var stream = _currAudioClient.CreatePCMStream(AudioApplication.Music))
            {
                try
                {
                    await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Started playback for file '{path}'."));

                    //stream audio with cancellation token for skipping
                    _currentAudioStreamTask = output.CopyToAsync(stream, _ffmpegCancelTokenSrc.Token);
                    await _currentAudioStreamTask;

                    await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Finished playback for file '{path}'."));
                }

                //don't allow cancellation exceptions to propogate
                catch (OperationCanceledException)
                {
                    await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Playback cancelled for file '{path}'."));

                    lock (_ffmpegCancelTokenSrc)
                    {
                        if (_ffmpegCancelTokenSrc.IsCancellationRequested)
                        {
                            //reset the cancel state to prevent skipping multiple songs
                            _ffmpegCancelTokenSrc.Dispose();
                            _ffmpegCancelTokenSrc = new CancellationTokenSource();
                        }
                    }
                }
                finally
                {
                    //clean up
                    output.Dispose();
                    stream.Clear();
                    ffmpeg.Kill(true);
                    _playing = false;
                }
            }
        }
        public async Task PlayRegexAudio(string filename)
        {
            //copy the queue and set the playing state
            _weedPlaying = true;

            //stop any currently active streams
            StopFfmpeg();

            if (_currAudioClient == null || _currAudioClient.ConnectionState != ConnectionState.Connected)
            {
                await JoinAudio();
            }

            string path = Path.Combine(AudioPath, filename);
            path = Path.GetFullPath(path);

            await SendAudioAsync(path);

            _weedPlaying = false;

            if (_songQueue.Count == 0)
            {
                await LeaveAudio();
            }
        }

        private Process CreateProcess(string path)
        {
            //start an ffmpeg process for the given song file
            return Process.Start(new ProcessStartInfo
            {
                FileName = FFMPEG_PROCESS_NAME,
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
        }
        #endregion

        #region queue control methods
        public async Task PlayNextInQueue(bool saveQueue = true)
        {
            while (_songQueue.Count > 0)
            {
                //fetch the next song in queue
                AudioItem nextInQueue;
                lock (_queueLock)
                {
                    nextInQueue = _songQueue.First.Value;
                    _songQueue.RemoveFirst();
                }

                //need to download if not already saved locally (change the URL to the path of the downloaded file)
                if (nextInQueue is YouTubeAudioItem)
                {
                    YouTubeAudioItem nextVideo = nextInQueue as YouTubeAudioItem;
                    try
                    {
                        //if the youtube video has not been downloaded yet
                        if (nextVideo.AudioSource == YouTubeAudioType.Url)
                        {
                            nextVideo.Path = await DownloadYoutubeVideoAsync(nextVideo.VideoUrl);
                        }
                    }
                    catch (ArgumentException)
                    {
                        await Logger.Log(new LogMessage(LogSeverity.Warning, "AudioSvc", $"failed to download local file for {nextVideo.DisplayName}, skipping..."));

                        //skip this item if the download fails
                        continue;
                    }
                }

                //set the current channel for the next song and join channel
                if (CurrentChannel == null)
                {
                    CurrentChannel = await Guild.GetVoiceChannelAsync(nextInQueue.PlayChannelId);
                    await JoinAudio();

                }
                else if (CurrentChannel.Id != nextInQueue.PlayChannelId)
                {
                    await LeaveAudio();
                    CurrentChannel = await Guild.GetVoiceChannelAsync(nextInQueue.PlayChannelId);
                    await JoinAudio();
                }

                //set the display name to file name if it's empty
                if (string.IsNullOrEmpty(nextInQueue.DisplayName))
                {
                    nextInQueue.DisplayName = Path.GetFileNameWithoutExtension(nextInQueue.Path);
                }

                //set bot client's status to the song name
                if (Client != null)
                {
                    await Client.SetGameAsync(nextInQueue.DisplayName);
                }

                //update the currently-playing song
                _currentSong = nextInQueue;

                //record current queue state 
                if (saveQueue)
                {
                    await SaveQueueContents();
                }

                _currentSong.StartTime = DateTime.Now;

                //begin playback
                await SendAudioAsync(nextInQueue.Path);

                //play next in queue (if any)
                //await PlayNextInQueue();
            }

            //out of songs, leave channel and clean up
            await LeaveAudio();
            if (Client != null)
            {
                await Client.SetGameAsync(null);
            }

            CleanAudioFiles();

        }

        public async Task MoveSongToFront(int index)
        {
            if (_songQueue.Count == 0)
            {
                await ParentModule.ServiceReplyAsync("There are no songs in the queue.");
                await Logger.Log(new LogMessage(LogSeverity.Warning, "AudioSvc", $"Cannot move song to front of queue (queue empty)."));
            }
            //need an offset of 2 because item 2 in song list is actually first item of queue
            else if (index > _songQueue.Count + 1 || index < 2)
            {
                await ParentModule.ServiceReplyAsync("The requested index was out of bounds.");
                await Logger.Log(new LogMessage(LogSeverity.Warning, "AudioSvc", $"Cannot move song to front of queue (index out of bounds)."));
            }
            else
            {
                //get the song at the requested index and remove it
                AudioItem moveSong;
                lock (_queueLock)
                {
                    moveSong = _songQueue.ElementAt(index - 2);
                    _songQueue.Remove(moveSong);
                }

                //insert it at the front of the queue
                await EnqueueSong(moveSong, false);

                await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Song '{moveSong.DisplayName}' moved to front of queue."));
            }
        }

        public async Task QueueTempSong(SocketUser owner, IReadOnlyCollection<Attachment> attachments, ulong channelId, bool append = true)
        {
            List<string> files = AttachmentHelper.DownloadAttachments(attachments);
            string path = files[0];
            string displayName = Path.GetFileName(path);

            await EnqueueSong(new LocalAudioItem() { Path = path, PlayChannelId = channelId, AudioSource = FileAudioType.Attachment, DisplayName = displayName, OwnerName = owner.Username }, append);

            await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Queued temp song '{displayName}'."));

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

        private async Task EnqueueSong(AudioItem item, bool append = true)
        {
            //put the item in the backup queue if weed is ongoing
            lock (_queueLock)
            {
                if (append)
                {
                    _songQueue.AddLast(item);
                }
                else
                {
                    _songQueue.AddFirst(item);
                }
            }

            string queueName = _weedPlaying == true ? "weed" : "main";
            string queueEnd = append == true ? "back" : "front";

            await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Added song '{item.DisplayName}' to {queueEnd} of {queueName} queue."));
        }

        public async Task QueueLocalSong(SocketUser owner, string path, ulong channelId, bool append = true)
        {
            string displayName = Path.GetFileNameWithoutExtension(path);
            await EnqueueSong(new LocalAudioItem() { Path = path, PlayChannelId = channelId, AudioSource = FileAudioType.Local, DisplayName = displayName, OwnerName = owner.Username }, append);

            if (!_playing)
            {
                await PlayNextInQueue();
            }
            else
            {
                await SaveQueueContents();
            }
        }

        public async Task QueueYoutubePlaylist(SocketUser owner, string playlistURL, ulong channelId, bool append = true)
        {
            //check if the given URL refers to a youtube playlist
            if (!PlaylistUrlIsValid(playlistURL))
            {
                await ParentModule.ServiceReplyAsync("The given URL is not a valid YouTube playlist.");
                await Logger.Log(new LogMessage(LogSeverity.Warning, "AudioSvc", $"URL '{playlistURL}' is not a valid youtube playlist URL."));
                return;
            }

            List<string> videoUrls = await GetYoutubePlaylistUrlsAsync(playlistURL);

            await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Fetched all video URLs for playlist URL '{playlistURL}'."));

            //add the list of URLs to the queue for downloading during playback
            await QueueYoutubeURLs(videoUrls, owner, channelId, append);
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

        private async Task QueueYoutubeURLs(List<string> urls, SocketUser owner, ulong channelId, bool append = true)
        {
            LinkedListNode<AudioItem> insertAtNode = null;
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

                //add the item to the front or back of the queue
                if (append)
                {
                    await EnqueueSong(currVideo);
                }
                else
                {
                    //add the item to the front of the queue (preserve order)
                    LinkedListNode<AudioItem> insertNode = new LinkedListNode<AudioItem>(currVideo);
                    lock (_queueLock)
                    {
                        if (insertAtNode != null)
                        {
                            _songQueue.AddAfter(insertAtNode, insertNode);
                        }
                        else
                        {
                            _songQueue.AddFirst(insertNode);
                        }
                    }

                    await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Added song '{currVideo.DisplayName}' to front of main queue."));

                    insertAtNode = insertNode;
                }
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

        public async Task QueueYoutubeSongPreDownloaded(SocketUser owner, string url, ulong channelId, bool append = true)
        {
            string displayName = await GetVideoTitleFromUrlAsync(url);

            //get a local file for the current video
            string filePath = await DownloadYoutubeVideoAsync(url);

            await EnqueueSong(new YouTubeAudioItem() { Path = filePath, VideoUrl = url, PlayChannelId = channelId, AudioSource = YouTubeAudioType.PreDownloaded, DisplayName = displayName, OwnerName = owner.Username }, append);

            if (!_playing)
            {
                await PlayNextInQueue();
            }
            else
            {
                await SaveQueueContents();
            }
        }
        #endregion

        #region queue/song state management methods
        public async Task LoadQueueContents(string filename = "queue-contents.json", bool weedLoad = true)
        {
            //try to load the queue state file
            string queueFilename = Path.Combine(AudioPath, "backup", filename);
            if (!File.Exists(queueFilename))
            {
                throw new FileNotFoundException("No queue backup file was found in the backup directory.");
            }

            using (StreamReader jsonReader = new StreamReader(queueFilename))
            {
                //read queue file contents
                string[] text = (await jsonReader.ReadToEndAsync()).Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                //reset the queue
                lock (_queueLock)
                {
                    _songQueue.Clear();
                }

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
                        await EnqueueSong(currItem);
                    }
                }

                await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Loaded queue contents ({text.Length - 1} songs)."));
            }

            //swap the "current" queued song with the weed song element
            if (weedLoad)
            {
                var currSong = _songQueue.First();
                _songQueue.RemoveFirst();
                var weedSong = _songQueue.First();
                _songQueue.RemoveFirst();
                _songQueue.AddFirst(currSong);
                _songQueue.AddFirst(weedSong);
            }

            if (!_playing)
            {
                await PlayNextInQueue();
            }
        }

        public async Task SaveQueueContents(string filename = "queue-contents.json")
        {
            //store the queue contents to file
            using (StreamWriter jsonWriter = new StreamWriter(Path.Combine(AudioPath, "backup", filename), false))
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
        #endregion

        #region weed
        public async Task PlayWeed()
        {
            await SaveQueueContents("weed-backup.json");

            ulong weedID = ulong.Parse(Config["WeedChannelId"]);
            await EnqueueSong(new LocalAudioItem() { Path = Path.Combine(AudioPath, "weedlmao.mp3"), PlayChannelId = weedID, AudioSource = FileAudioType.Local, DisplayName = "weed", OwnerName = "Terminus.NET" }, false);
            await SaveQueueContents("weed-queue.json");
            if (!_playing)
            {
                await PlayNextInQueue(false);
            }
            else
            {
                await StopAllAudio();
                await LoadQueueContents("weed-queue.json", true);
            }
        }
        #endregion

        #region song display methods
        public async Task<Embed> DisplayCurrentSong()
        {
            if (_currentSong == null)
            {
                await ParentModule.ServiceReplyAsync($"No song is currently playing.");
                return null;
            }

            //create an embed with info about the currently playing song
            EmbedBuilder builder = new EmbedBuilder()
            {
                Title = $"Currently Playing: {_currentSong.DisplayName}"
            };

            builder.AddField("Source: ", GetAudioSourceString(_currentSong));

            //add time info
            TimeSpan elapsedTime = DateTime.Now - _currentSong.StartTime;
            builder.AddField("Time started: ", _currentSong.StartTime.ToString("hh\\:mm"));
            builder.AddField("Time playing: ", elapsedTime.ToString("mm\\:ss"));

            return builder.Build();
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
                string displayName = _currentSong.DisplayName;
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = Path.GetFileNameWithoutExtension(_currentSong.Path);
                }
                string songSource = GetAudioSourceString(_currentSong);
                embed.AddField($"{entryCount + 1}: {displayName} **(currently playing)**", songSource);
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

                string displayName = songItem.DisplayName;
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = Path.GetFileNameWithoutExtension(songItem.Path);
                }

                //add the current queue item to the song list 
                string songName = $"**{entryCount + 1}:** {displayName}";
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
        #endregion

        #region Youtube helpers
        private async Task<string> DownloadYoutubeVideoAsync(string url)
        {
            //define the directory to save video files to
            string tempPath = Path.Combine(Environment.CurrentDirectory, "assets", "temp");
            string videoDataFullPath;

            try
            {
                //download the youtube video data (usually .mp4 or .webm)
                var youtube = YouTube.Default;
                var video = await youtube.GetVideoAsync(url);
                var videoData = await video.GetBytesAsync();

                await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Downloaded youtube video '{video.FullName}'."));

                //give the video file a unique name to prevent collisions
                //  **if libvideo fails to fetch the video's title, it names the file 'YouTube.mp4'**
                string videoDataFilename = $"{Guid.NewGuid().ToString("N")}{Path.GetExtension(video.FullName)}";

                //write the downloaded media file to the temp assets dir
                videoDataFullPath = Path.Combine(tempPath, videoDataFilename);
                await File.WriteAllBytesAsync(videoDataFullPath, videoData);

                return videoDataFullPath;
            }
            catch (Exception ex)
            {
                //give a more helpful error message
                throw new ArgumentException($"Could not download a video file for the given URL: '{ex.Message}'.", ex);
            }
        }

        private async Task<List<string>> GetYoutubePlaylistUrlsAsync(string playlistUrl)
        {
            List<string> videoUrls = new List<string>();
            string nextPageToken = "";

            //iterate over paginated playlist results from youtube and extract video URLs
            while (nextPageToken != null)
            {
                //prepare a paged playlist request for the given playlist URL
                var playlistRequest = _ytService.PlaylistItems.List("snippet,contentDetails");
                playlistRequest.PlaylistId = GetPlaylistIdFromUrl(playlistUrl);
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

            return videoUrls;
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
        #endregion

        #region misc. helpers
        private void CleanAudioFiles()
        {
            AttachmentHelper.DeleteFiles(AttachmentHelper.GetTempAssets("*.mp3"));
            AttachmentHelper.DeleteFiles(AttachmentHelper.GetTempAssets("*.mp4"));
            AttachmentHelper.DeleteFiles(AttachmentHelper.GetTempAssets("*.webm"));
        }
        public void SetGuildClient(IGuild g, DiscordSocketClient c)
        {
            Guild = g;
            Client = c;
        }
        #endregion

        #region Hideki ZONE
        public async Task AddRandomHidekiSong(SocketUser owner, ulong channelId, bool append = true)
        {
            //check if we need to reload
            if (DateTime.Now.Subtract(_lastHidekiReload).TotalHours > 1.0)
            {
                await LoadHidekiSongsCache();
                _lastHidekiReload = DateTime.Now;
            }

            //choose random hideki video URL
            string randomVideoUrl = _hidekiSongsCache[_random.Next(_hidekiSongsCache.Count)];

            await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Selected Hideki video url: {randomVideoUrl}"));
            try
            {
                await QueueYoutubeSongPreDownloaded(owner, randomVideoUrl, channelId, append);
            }
            catch (ArgumentException)
            {
                await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Video url is invalid, retrying..."));
                await AddRandomHidekiSong(owner, channelId, append);
            }
        }

        private async Task LoadHidekiSongsCache()
        {
            //load playlist URL from config
            var playlistUrls = Config.GetSection("HidekiPlaylists").GetChildren();
            foreach (var playlistUrlSection in playlistUrls)
            {
                string currPlaylistUrl = playlistUrlSection.Value;
                List<string> songs = await GetYoutubePlaylistUrlsAsync(currPlaylistUrl);
                _hidekiSongsCache.AddRange(songs);
            }
        }
        #endregion
    }
}
