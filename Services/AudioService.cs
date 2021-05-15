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
using Google.Apis.YouTube.v3;
using Google.Apis.Auth.OAuth2;
using System.Threading;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using YoutubeExplode;
using Quartz;
using Quartz.Impl.Matchers;

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
        private readonly object _cancelLock = new object();

        //cache hideki playlist songs to prevent too many API calls
        private List<string> _hidekiSongsCache = new List<string>();

        private DateTime _lastHidekiReload = DateTime.MinValue;

        //metadata about the currently playing song
        private AudioItem _currentSong = null;

        //currently-connected channel (needs to be set from outside the service occasionally)
        public IVoiceChannel CurrentChannel { get; set; } = null;

        //used for streaming audio in the currently-connected channel
        private IAudioClient _currAudioClient = null;

        //tokens for cancelling playback
        private CancellationTokenSource _ffmpegCancelTokenSrc = new CancellationTokenSource();

        //JSON converter settings for queue save/load
        private static readonly JsonSerializerSettings JSON_SETTINGS = new JsonSerializerSettings()
        {
            Formatting = Formatting.None,
            TypeNameHandling = TypeNameHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        //state flags
        private bool _playing = false;

        //weed timer
        private readonly Timer _timer;

        private YouTubeService _ytService;

        private IYoutubeDownloader _ytDownloader = new YoutubeExplodeDownloader();

        private Dictionary<string, Type> _youtubeDownloaders = new Dictionary<string, Type>();

        //Discord info objects
        public IGuild Guild { get; set; }
        public DiscordSocketClient Client { get; set; }

        //path for local (aliased) audio files
        public string AudioPath { get; } = Path.Combine("assets", "audio");
        public string TempPath { get; } = Path.Combine("assets", "temp");
        public string RadioPath { get; } = Path.Combine("assets", "audio", "playlists");
        public string EventsPath { get; } = Path.Combine("assets", "audio", "events");

        //RNG
        private Random _random;

        //Quartz scheduler
        private IScheduler _scheduler;
        #endregion

        #region init
        public AudioService(IConfiguration config, Random random, IScheduler scheduler)
        {
            _random = random;
            Config = config;
            _scheduler = scheduler;

            //init weed timer
            DateTime now = DateTime.Now;
            DateTime fourTwenty = DateTime.Today.AddHours(16).AddMinutes(20);

            if (now > fourTwenty)
            {
                fourTwenty = fourTwenty.AddDays(1.0);
            }
            int fourTwentyMs = (int)(fourTwenty - now).TotalMilliseconds;
            _timer = new Timer(async _ => await PlayWeed(), null, fourTwentyMs, (int)new TimeSpan(24, 0, 0).TotalMilliseconds);

            Task.Run(async () => await InitYoutubeService());
            Task.Run(async () => await LoadAllAudioEvents());

            //init youtube downloader dict
            _youtubeDownloaders.Add("libvideo", typeof(LibVideoDownloader));
            _youtubeDownloaders.Add("yt-explode", typeof(YoutubeExplodeDownloader));
        }

        private async Task InitYoutubeService()
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

        public async Task StopFfmpeg()
        {
            //stop any currently active streams
            if (_ffmpegCancelTokenSrc != null)
            {
                lock (_cancelLock)
                {
                    _ffmpegCancelTokenSrc.Cancel();
                }
                await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", "ffmpeg token canceled."));
            }
        }

        public async Task JoinAudio(int retryCount = 3)
        {
            try
            {
                await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Connecting to channel '{CurrentChannel.Name}'..."));
                _currAudioClient = await CurrentChannel.ConnectAsync();
                _currAudioClient.Disconnected += _currAudioClient_Disconnected;
                await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Joined audio on channel '{CurrentChannel.Name}'."));
            }

            //attempt to rejoin on timeout
            catch (TimeoutException)
            {
                if (retryCount == 0)
                {
                    await Logger.Log(new LogMessage(LogSeverity.Error, "AudioSvc", $"failed to connect to voice channel after repeated timeouts."));
                    await StopAllAudio();
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

        private async Task _currAudioClient_Disconnected(Exception arg)
        {
            //update state to keep queue from burning through songs
            /*NOTE: I can't really find an elegant way to pause the song queue
                    when a disconnect occurs. This means that the queue will sometimes
                    burn through a song or two (with failed playback since it's not in 
                    channel) before this disconnect handler fires and lets the queue
                    know that it needs to reconnect to voice.*/
            CurrentChannel = null;
            if (arg is OperationCanceledException)
            {
                await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Routine audio client disconnect."));
            }
            else
            {
                await Logger.Log(new LogMessage(LogSeverity.Warning, "AudioSvc", $"Exception caused audio client disconnect: {arg.Message}"));
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
                await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Disconnecting from channel '{CurrentChannel.Name}'."));
                await CurrentChannel.DisconnectAsync();
                CurrentChannel = null;
            }
            if (_currAudioClient != null)
            {
                await _currAudioClient.StopAsync();
                _currAudioClient = null;
            }
        }

        private async Task StreamFfmpegAudio(string path)
        {
            //init ffmpeg and audio streams
            using (var ffmpeg = CreateFfmpegProcess(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var stream = _currAudioClient.CreatePCMStream(AudioApplication.Music))
            {
                try
                {
                    await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Spawned ffmpeg process {ffmpeg.Id}."));
                    await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Started playback for song '{_currentSong.DisplayName}' ({Path.GetFileName(path)})."));

                    //stream audio with cancellation token for skipping
                    _playing = true;
                    await output.CopyToAsync(stream, _ffmpegCancelTokenSrc.Token);
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    //make sure the current audio process is killed to prevent overlapping playback
                    ffmpeg.Kill();
                    await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Killed ffmpeg process {ffmpeg.Id}."));

                    await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Finished playback."));
                    _playing = false;

                    // reset cancellation token source
                    lock (_cancelLock)
                    {
                        _ffmpegCancelTokenSrc.Dispose();
                        _ffmpegCancelTokenSrc = new CancellationTokenSource();
                    }
                }
            }
        }
        public async Task PlayRegexAudio(string filename)
        {
            string path = Path.Combine(AudioPath, filename);
            path = Path.GetFullPath(path);

            ulong playID = ulong.Parse(Config["AudioChannelId"]);
            await EnqueueSong(new LocalAudioItem() { Path = path, PlayChannelId = playID, AudioSource = FileAudioType.Local, DisplayName = Path.GetFileName(path), OwnerName = "Terminus.NET" }, false);
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

        private Process CreateFfmpegProcess(string path)
        {
            //start an ffmpeg process for the given song file
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
        }
        #endregion

        #region queue control methods
        public async Task StartQueueIfIdle()
        {
            if (!_playing)
            {
                //start playing if idle
                await PlayNextInQueue();
            }
            else
            {
                //save if already playing
                await SaveQueueContents();
            }
        }

        public async Task PlayNextInQueue(bool saveQueue = true)
        {
            try
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

                    //set the display name to file name if it's empty
                    if (string.IsNullOrEmpty(nextInQueue.DisplayName))
                    {
                        nextInQueue.DisplayName = Path.GetFileNameWithoutExtension(nextInQueue.Path);
                    }

                    await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Removed song '{nextInQueue.DisplayName}' from queue."));

                    //need to download if not already saved locally (change the URL to the path of the downloaded file)
                    if (nextInQueue is YouTubeAudioItem)
                    {
                        YouTubeAudioItem nextVideo = nextInQueue as YouTubeAudioItem;

                        //if the youtube video has not been downloaded yet
                        if (nextVideo.AudioSource == YouTubeAudioType.Url)
                        {
                            try
                            {
                                nextVideo.Path = await DownloadYoutubeVideoAsync(nextVideo.VideoUrl);
                            }
                            catch (ArgumentException)
                            {
                                await Logger.Log(new LogMessage(LogSeverity.Warning, "AudioSvc", $"failed to download local file for {nextVideo.DisplayName}, skipping..."));

                                //skip this item if the download fails
                                continue;
                            }
                        }
                    }

                    //join channel if not in channel
                    if (CurrentChannel == null)
                    {
                        CurrentChannel = await Guild.GetVoiceChannelAsync(nextInQueue.PlayChannelId);
                        await JoinAudio();

                    }
                    //switch channels if the current song was queued for another channel
                    else if (CurrentChannel.Id != nextInQueue.PlayChannelId)
                    {
                        await LeaveAudio();
                        CurrentChannel = await Guild.GetVoiceChannelAsync(nextInQueue.PlayChannelId);
                        await JoinAudio();
                    }

                    //set bot client's status to the song name
                    if (Client != null)
                    {
                        await Client.SetGameAsync(nextInQueue.DisplayName, null, ActivityType.Listening);
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
                    await StreamFfmpegAudio(nextInQueue.Path);
                }
            }
            finally
            {
                //out of songs, leave channel and clean up
                await LeaveAudio();

                CleanAudioFiles();
            }
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

            await StartQueueIfIdle();
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

            string queueEnd = append == true ? "back" : "front";
            await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Added song '{item.DisplayName}' to {queueEnd} of queue."));
        }

        public async Task QueueLocalSong(SocketUser owner, string path, ulong channelId, bool append = true)
        {
            string displayName = Path.GetFileNameWithoutExtension(path);
            await EnqueueSong(new LocalAudioItem() { Path = path, PlayChannelId = channelId, AudioSource = FileAudioType.Local, DisplayName = displayName, OwnerName = owner.Username }, append);

            await StartQueueIfIdle();
        }

        public async Task QueueYoutubePlaylist(SocketUser owner, string playlistURL, ulong channelId, bool append = true, bool shuffle = false)
        {
            //check if the given URL refers to a youtube playlist
            if (!PlaylistUrlIsValid(playlistURL))
            {
                await ParentModule.ServiceReplyAsync("The given URL is not a valid YouTube playlist.");
                await Logger.Log(new LogMessage(LogSeverity.Warning, "AudioSvc", $"URL '{playlistURL}' is not a valid youtube playlist URL."));
                return;
            }

            List<string> videoUrls = await GetYoutubePlaylistUrlsAsync(playlistURL);

            //shuffle w/ Fisher-Yates algo if requested
            if (shuffle)
            {
                for (int i = videoUrls.Count - 1; i >= 1; i--)
                {
                    int swapIndex = _random.Next(0, i);
                    string temp = videoUrls[i];
                    videoUrls[i] = videoUrls[swapIndex];
                    videoUrls[swapIndex] = temp;
                }
            }

            await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Fetched all video URLs for playlist URL '{playlistURL}'."));

            //add the list of URLs to the queue for downloading during playback
            await QueueYoutubeURLs(videoUrls, owner, channelId, append);
        }

        public async Task QueueSearchedYoutubeSong(SocketUser owner, string searchTerm, ulong channelId, bool append)
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
                    await QueueYoutubeSongPreDownloaded(owner, url, channelId, append);

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

            await StartQueueIfIdle();
        }

        public async Task QueueYoutubeSongPreDownloaded(SocketUser owner, string url, ulong channelId, bool append = true)
        {
            string displayName = await GetVideoTitleFromUrlAsync(url);

            //get a local file for the current video
            string filePath = await DownloadYoutubeVideoAsync(url);

            await EnqueueSong(new YouTubeAudioItem() { Path = filePath, VideoUrl = url, PlayChannelId = channelId, AudioSource = YouTubeAudioType.PreDownloaded, DisplayName = displayName, OwnerName = owner.Username }, append);

            await StartQueueIfIdle();
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
            await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Saved queue contents."));
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

            await ParentModule.ServiceReplyAsync($"Added alias '{alias}' for {newPath}.");
        }
        #endregion

        #region radio commands
        private bool IsUserWhitelisted(SocketUser user, RadioPlaylist playlist)
        {
            //check if the user is in the playlist's whitelist
            return playlist.WhitelistUsers.Contains(user.Username);
        }

        private bool IsUserWhitelisted(SocketUser user, string playlistName)
        {
            string playlistFilename = $"radio-{playlistName}.json";
            RadioPlaylist playlist = JsonConvert.DeserializeObject<RadioPlaylist>(File.ReadAllText(Path.Combine(RadioPath, playlistFilename)), JSON_SETTINGS);

            return IsUserWhitelisted(user, playlist);
        }

        private string GetPlaylistFilename(string playlistName)
        {
            return Path.Combine(RadioPath, $"radio-{playlistName}.json");
        }

        private async Task<RadioPlaylist> LoadPlaylistFromFile(string playlistFilename)
        {
            return JsonConvert.DeserializeObject<RadioPlaylist>(await File.ReadAllTextAsync(playlistFilename), JSON_SETTINGS);
        }

        private async Task SavePlaylistToFile(RadioPlaylist playlist, string playlistFilename)
        {
            await File.WriteAllTextAsync(playlistFilename, JsonConvert.SerializeObject(playlist, JSON_SETTINGS));
        }

        public async Task AddRadioSong(SocketUser owner, string playlistName, string youtubeUrl)
        {
            string playlistFilename = GetPlaylistFilename(playlistName);
            //check if there is a playlist file with the given name
            if (!File.Exists(playlistFilename))
            {
                await ParentModule.ServiceReplyAsync($"No playlist was found for the given name: `{playlistName}`.");
                return;
            }

            //check if the user is whitelisted for the current playlist
            if (!IsUserWhitelisted(owner, playlistName))
            {
                await ParentModule.ServiceReplyAsync($"You aren't whitelisted for the playlist `{playlistName}`.");
                return;
            }

            //create a new youtube audio item to save
            string displayName = await GetVideoTitleFromUrlAsync(youtubeUrl);
            ulong channelId = ulong.Parse(Config["AudioChannelId"]);
            YouTubeAudioItem newSong = new YouTubeAudioItem()
            {
                VideoUrl = youtubeUrl,
                PlayChannelId = channelId,
                AudioSource = YouTubeAudioType.Url,
                DisplayName = displayName,
                OwnerName = owner.Username
            };

            //add the song and save the playlist
            RadioPlaylist currPlaylist = await LoadPlaylistFromFile(playlistFilename);
            currPlaylist.Songs.AddLast(newSong);
            await SavePlaylistToFile(currPlaylist, playlistFilename);

            await ParentModule.ServiceReplyAsync($"Added song `{newSong.DisplayName}` to `{currPlaylist.Name}`.");
        }

        public async Task DeleteRadioSong(SocketUser owner, string playlistName, int index)
        {
            string playlistFilename = GetPlaylistFilename(playlistName);
            //check if there is a playlist file with the given name
            if (!File.Exists(playlistFilename))
            {
                await ParentModule.ServiceReplyAsync($"No playlist was found for the given name: `{playlistName}`.");
                return;
            }

            //check if the user is whitelisted for the current playlist
            if (!IsUserWhitelisted(owner, playlistName))
            {
                await ParentModule.ServiceReplyAsync($"You aren't whitelisted for the playlist `{playlistName}`.");
                return;
            }

            //attempt to remove the indexed song from the playlist (assume playlist is 1-indexed)
            index--;
            RadioPlaylist playlist = await LoadPlaylistFromFile(playlistFilename);
            if (index < 0 || index > playlist.Songs.Count)
            {
                await ParentModule.ServiceReplyAsync($"The given index was out of bounds for the playlist `{playlistName}` ({playlist.Songs.Count} songs).");
                return;
            }

            //find the song by index and remove it 
            int currIndex = 0;
            LinkedListNode<YouTubeAudioItem> currNode = playlist.Songs.First;
            while (currIndex != index && currNode != null)
            {
                currIndex++;
                currNode = currNode.Next;
            }
            playlist.Songs.Remove(currNode);

            //save updated playlist to file
            await SavePlaylistToFile(playlist, playlistFilename);

            await ParentModule.ServiceReplyAsync($"Deleted song `{currNode.Value.DisplayName}` from `{playlist.Name}`.");
        }

        public async Task CreateRadioPlaylist(SocketUser owner, string playlistName)
        {
            string playlistFilename = GetPlaylistFilename(playlistName);
            if (File.Exists(playlistFilename))
            {
                await ParentModule.ServiceReplyAsync($"A playlist with the name `{playlistName}` already exists.");
            }

            //create playlist object and save to file
            RadioPlaylist newPlaylist = new RadioPlaylist()
            {
                Name = playlistName,
                OwnerName = owner.Username,
                WhitelistUsers = new List<string>() { owner.Username },
                Songs = new LinkedList<YouTubeAudioItem>()
            };
            await SavePlaylistToFile(newPlaylist, playlistFilename);

            await ParentModule.ServiceReplyAsync($"Created new playlist `{newPlaylist.Name}`.");
        }

        public async Task RemoveWhitelistUserFromRadioPlaylist(string playlistName, SocketUser commandUser, SocketUser blacklistUser)
        {
            string playlistFilename = GetPlaylistFilename(playlistName);
            if (!File.Exists(playlistFilename))
            {
                await ParentModule.ServiceReplyAsync($"No playlist was found for the given name: `{playlistName}`.");
                return;
            }

            RadioPlaylist playlist = await LoadPlaylistFromFile(playlistFilename);

            //only allow owner to whitelist other users
            if (playlist.OwnerName != commandUser.Username)
            {
                await ParentModule.ServiceReplyAsync($"Only the playlist owner (`{playlist.OwnerName}`) can remove users from the whitelist.");
                return;
            }

            //don't add users that are already whitelisted
            if (!IsUserWhitelisted(blacklistUser, playlist))
            {
                await ParentModule.ServiceReplyAsync($"The user `{blacklistUser.Username}` is not on the whitelist for `{playlistName}.`");
                return;
            }

            //remove user from whitelist and save to file
            playlist.WhitelistUsers.Remove(blacklistUser.Username);
            await SavePlaylistToFile(playlist, playlistFilename);

            await ParentModule.ServiceReplyAsync($"Removed user `{blacklistUser.Username}` from whitelist for `{playlist.Name}`.");
        }

        public async Task WhitelistUserForRadioPlaylist(string playlistName, SocketUser commandUser, SocketUser whitelistUser)
        {
            string playlistFilename = GetPlaylistFilename(playlistName);
            if (!File.Exists(playlistFilename))
            {
                await ParentModule.ServiceReplyAsync($"No playlist was found for the given name: `{playlistName}`.");
                return;
            }

            RadioPlaylist whitelistPlaylist = await LoadPlaylistFromFile(playlistFilename);

            //only allow owner to whitelist other users
            if (whitelistPlaylist.OwnerName != commandUser.Username)
            {
                await ParentModule.ServiceReplyAsync($"Only the playlist owner (`{whitelistPlaylist.OwnerName}`) can whitelist users.");
                return;
            }

            //don't add users that are already whitelisted
            if (IsUserWhitelisted(whitelistUser, whitelistPlaylist))
            {
                await ParentModule.ServiceReplyAsync($"The user `{whitelistUser.Username}` is already whitelisted for `{playlistName}.`");
                return;
            }

            //add user to whitelist and save to file
            whitelistPlaylist.WhitelistUsers.Add(whitelistUser.Username);
            await SavePlaylistToFile(whitelistPlaylist, playlistFilename);

            await ParentModule.ServiceReplyAsync($"Added user `{whitelistUser.Username}` to whitelist for `{whitelistPlaylist.Name}`.");
        }


        public async Task DeleteRadioPlaylist(SocketUser user, string playlistName)
        {
            //can't delete that which does not exist
            string playlistFilename = GetPlaylistFilename(playlistName);
            if (!File.Exists(playlistFilename))
            {
                await ParentModule.ServiceReplyAsync($"No playlist was found for the given name: `{playlistName}`.");
                return;
            }

            RadioPlaylist deletePlaylist = await LoadPlaylistFromFile(playlistFilename);

            //only allow playlist owner to delete
            if (deletePlaylist.OwnerName != user.Username)
            {
                await ParentModule.ServiceReplyAsync($"Only the playlist owner (`{deletePlaylist.OwnerName}`) can delete `{deletePlaylist.Name}`.");
                return;
            }

            File.Delete(playlistFilename);
            await ParentModule.ServiceReplyAsync($"Deleted playlist `{deletePlaylist.Name}`.");
        }

        public async Task LoadRadioPlaylist(SocketUser owner, string playlistName, bool shuffle, bool append)
        {
            string playlistFilename = GetPlaylistFilename(playlistName);
            if (!File.Exists(playlistFilename))
            {
                await ParentModule.ServiceReplyAsync($"No playlist was found for the given name: `{playlistName}`.");
                return;
            }

            RadioPlaylist loadPlaylist = await LoadPlaylistFromFile(playlistFilename);
            if (shuffle)
            {
                loadPlaylist.ShuffleSongs();
            }

            LinkedListNode<AudioItem> insertAtNode = null;
            foreach (YouTubeAudioItem song in loadPlaylist.Songs)
            {
                if (append)
                {
                    await EnqueueSong(song);
                }
                else
                {
                    //add the item to the front of the queue (preserve order)
                    LinkedListNode<AudioItem> insertNode = new LinkedListNode<AudioItem>(song);
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

                    await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Added song '{song.DisplayName}' to front of main queue."));

                    insertAtNode = insertNode;
                }
            }

            await StartQueueIfIdle();
        }

        public async Task ShowRadioPlaylistContents(string playlistName)
        {
            string playlistFilename = GetPlaylistFilename(playlistName);
            if (!File.Exists(playlistFilename))
            {
                await ParentModule.ServiceReplyAsync($"No playlist was found for the given name: `{playlistName}`.");
                return;
            }

            RadioPlaylist playlist = await LoadPlaylistFromFile(playlistFilename);
            List<Embed> playlistContents = ListRadioPlaylistContents(playlist);

            foreach (Embed embed in playlistContents)
            {
                await ParentModule.ServiceReplyAsync(embed: embed);
            }
        }

        public async Task ShowAllRadioPlaylists()
        {
            List<Embed> playlists = ListRadioPlaylists();

            foreach (Embed embed in playlists)
            {
                await ParentModule.ServiceReplyAsync(embed: embed);
            }
        }
        #endregion

        #region audio events
        public async Task SaveAudioEvent(string songName, string cronString, ulong channelId)
        {
            AudioEventState state = new AudioEventState()
            {
                SongName = songName,
                CronString = cronString,
                ChannelId = channelId
            };
            string eventFilename = Path.Combine(EventsPath, $"{songName}.json");
            await File.WriteAllTextAsync(eventFilename, JsonConvert.SerializeObject(state, JSON_SETTINGS));
        }

        public async Task<AudioEventState> LoadAudioEvent(string songName)
        {
            string eventFilename = Path.Combine(EventsPath, $"{songName}.json");
            if (!File.Exists(eventFilename))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<AudioEventState>(await File.ReadAllTextAsync(eventFilename));
        }

        public async Task LoadAllAudioEvents()
        {
            foreach (var file in Directory.GetFiles(EventsPath))
            {
                AudioEventState state = await LoadAudioEvent(Path.GetFileNameWithoutExtension(file));
                await CreateAudioEvent(state.SongName, state.CronString, state.ChannelId);
            }
        }

        public async Task CreateAudioEvent(string songName, string cronString, ulong channelId)
        {
            //start scheduler if needed
            if (!_scheduler.IsStarted)
            {
                await _scheduler.Start();
                await Logger.Log(new LogMessage(LogSeverity.Info, "Scheduler", $"Started scheduler."));

            }

            //create job (pass cron string, song name, channel ID)
            IJobDetail job = JobBuilder.Create<AudioEventJob>()
                .WithIdentity(songName, "group1")
                .UsingJobData("SongName", songName)
                .UsingJobData("ChannelId", channelId.ToString())
                .Build();

            //create trigger from cron string
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(Guid.NewGuid().ToString("N"), "group1")
                .WithCronSchedule(cronString)
                .ForJob(songName, "group1")
                .Build();

            //save to file 
            await SaveAudioEvent(songName, cronString, channelId);
            DateTimeOffset triggerOffset = await _scheduler.ScheduleJob(job, trigger);
            await Logger.Log(new LogMessage(LogSeverity.Info, "Scheduler", $"Scheduled audio event '{songName}' in channel '{channelId}' at {triggerOffset}."));
        }
        private async Task<List<Embed>> ListAudioEvents()
        {
            IReadOnlyCollection<string> groupNames = await _scheduler.GetJobGroupNames();

            //need a list of embeds since each embed can only have 25 fields max
            List<Embed> jobList = new List<Embed>();
            int entryCount = 0;

            var embed = new EmbedBuilder
            {
                Title = $"Audio Events"
            };

            foreach (string groupName in groupNames)
            {
                GroupMatcher<JobKey> groupMatcher = GroupMatcher<JobKey>.GroupContains(groupName);
                IReadOnlyCollection<JobKey> jobKeys = await _scheduler.GetJobKeys(groupMatcher);
                foreach (JobKey jobKey in jobKeys)
                {
                    var jobDetail = await _scheduler.GetJobDetail(jobKey);
                    IReadOnlyCollection<ITrigger> triggers = await _scheduler.GetTriggersOfJob(jobKey);
                    entryCount++;

                    //if we have 25 entries in an embed already, need to make a new one 
                    if (entryCount % EmbedBuilder.MaxFieldCount == 0 && entryCount > 0)
                    {
                        jobList.Add(embed.Build());
                        embed = new EmbedBuilder();
                    }

                    string jobName = jobKey.Name;
                    string nextJobTime = triggers.FirstOrDefault().GetNextFireTimeUtc().GetValueOrDefault().ToLocalTime().ToString();

                    embed.AddField(jobName, nextJobTime);
                }
            }
 

            //add the most recently built embed if it's not in the list yet 
            if (jobList.Count == 0 || !jobList.Contains(embed.Build()))
            {
                jobList.Add(embed.Build());
            }

            return jobList;
        }

        public async Task ShowAudioEvents()
        {
            List<Embed> jobs = await ListAudioEvents();
            foreach (Embed embed in jobs)
            {
                await ParentModule.ServiceReplyAsync(embed: embed);
            }
        }

        public async Task PlayAudioEvent(LocalAudioItem audioEvent)
        {
            await EnqueueSong(audioEvent);
            await SaveQueueContents("event-queue.json");
            if (!_playing)
            {
                await PlayNextInQueue(false);
            }
            else
            {
                //load the current queue with the weed song added to the front of it
                await StopAllAudio();
                await LoadQueueContents("event-queue.json", true);
            }
        }
        #endregion

        #region weed
        public async Task PlayWeed()
        {
            //get the weed channel ID and queue the weed song
            ulong weedID = ulong.Parse(Config["WeedChannelId"]);
            LocalAudioItem weedEvent = new LocalAudioItem() 
            { 
                Path = Path.Combine(AudioPath, "weedlmao.mp3"), 
                PlayChannelId = weedID, AudioSource = FileAudioType.Local, 
                DisplayName = "weed", 
                OwnerName = "Terminus.NET" 
            };
            await PlayAudioEvent(weedEvent);
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

        public List<Embed> ListRadioPlaylistContents(RadioPlaylist playlist)
        {
            //need a list of embeds since each embed can only have 25 fields max
            List<Embed> songList = new List<Embed>();
            int numSongs = playlist.Songs.Count;
            int entryCount = 0;

            var embed = new EmbedBuilder
            {
                Title = $"{playlist.Name} by {playlist.OwnerName} ({numSongs} songs)"
            };

            foreach (YouTubeAudioItem item in playlist.Songs)
            {
                entryCount++;

                //if we have 25 entries in an embed already, need to make a new one 
                if (entryCount % EmbedBuilder.MaxFieldCount == 0 && entryCount > 0)
                {
                    songList.Add(embed.Build());
                    embed = new EmbedBuilder();
                }

                string displayName = item.DisplayName;
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = Path.GetFileNameWithoutExtension(item.Path);
                }

                //add the current queue item to the song list 
                string songName = $"**{entryCount}:** {displayName}";
                string songSource = GetAudioSourceString(item);

                embed.AddField(songName, songSource);
            }

            //add the most recently built embed if it's not in the list yet 
            if (songList.Count == 0 || !songList.Contains(embed.Build()))
            {
                songList.Add(embed.Build());
            }

            //add an embed for whitelisted users
            EmbedBuilder whitelistEmbed = new EmbedBuilder
            {
                Title = "Whitelisted Users"
            };
            int i = 1;
            foreach (string whitelistUsername in playlist.WhitelistUsers)
            {
                whitelistEmbed.AddField(name: i.ToString(), value: whitelistUsername);
                i++;
            }
            songList.Add(whitelistEmbed.Build());

            return songList;
        }

        public List<Embed> ListRadioPlaylists()
        {
            //need a list of embeds since each embed can only have 25 fields max
            var playlistFiles = Directory.GetFiles(RadioPath);
            List<Embed> playlists = new List<Embed>();
            int entryCount = 0;

            var embed = new EmbedBuilder
            {
                Title = $"{playlistFiles.Count()} Playlists"
            };

            foreach (string playlistFile in playlistFiles)
            {
                RadioPlaylist playlist = JsonConvert.DeserializeObject<RadioPlaylist>(File.ReadAllText(playlistFile), JSON_SETTINGS);
                entryCount++;

                //if we have 25 entries in an embed already, need to make a new one 
                if (entryCount % EmbedBuilder.MaxFieldCount == 0 && entryCount > 0)
                {
                    playlists.Add(embed.Build());
                    embed = new EmbedBuilder();
                }

                //add the current queue item to the song list 
                string songName = $"**{entryCount}:** {playlist.Name}";

                embed.AddField(songName, $"owner: {playlist.OwnerName}");
            }

            //add the most recently built embed if it's not in the list yet 
            if (playlists.Count == 0 || !playlists.Contains(embed.Build()))
            {
                playlists.Add(embed.Build());
            }

            return playlists;
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
        public async Task SwitchYoutubeDownloaderLibrary(string libName)
        {
            if (!_youtubeDownloaders.ContainsKey(libName))
            {
                await ParentModule.ServiceReplyAsync($"Invalid library name: `{libName}`");
                return;
            }

            Type downloaderType = _youtubeDownloaders[libName];
            _ytDownloader = (IYoutubeDownloader)Activator.CreateInstance(downloaderType);

            await ParentModule.ServiceReplyAsync($"Now using YouTube downloader `{libName}`.");
        }

        public string GetYoutubeDownloaderName()
        {
            Type downloaderType = _ytDownloader.GetType();
            return downloaderType.Name;
        }

        private async Task<string> DownloadYoutubeVideoAsync(string url)
        {
            try
            {
                await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"Downloading file from '{url}'."));
                return await _ytDownloader.DownloadYoutubeVideoAsync(url);
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

        public string GetAliasedSongPath(string aliasName)
        {
            foreach (string line in File.ReadAllLines(Path.Combine(AudioPath, "audioaliases.txt")))
            {
                if (line.StartsWith("#") || String.IsNullOrEmpty(line))
                {
                    continue;
                }
                string[] currEntry = line.Split(" ");
                if (currEntry[0] == aliasName)
                {
                    return Path.Combine(AudioPath, currEntry[1]);
                }
            }
            
            return null;
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
