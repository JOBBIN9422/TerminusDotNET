using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using TerminusDotNetCore.Modules;
using System;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.FileExtensions;
using System.Collections.Generic;
using TerminusDotNetCore.Helpers;
using MediaToolkit;
using MediaToolkit.Model;
using VideoLibrary;

namespace TerminusDotNetCore.Services
{
    public class AudioService : ICustomService
    {
        public IServiceModule ParentModule { get; set; }
        private ConcurrentQueue<AudioItem> songQueue = new ConcurrentQueue<AudioItem>();
        private ConcurrentQueue<AudioItem> backupQueue = new ConcurrentQueue<AudioItem>();
        private bool playing = false;
        private bool weedStarted = false;
        private bool weedPlaying = false;
        public IGuild guild { get; set; }
        public DiscordSocketClient _client;

        private readonly ConcurrentDictionary<ulong, Tuple<IAudioClient, IVoiceChannel>> ConnectedChannels = new ConcurrentDictionary<ulong, Tuple<IAudioClient, IVoiceChannel>>();

        public async void setGuildClient(IGuild g, DiscordSocketClient c)
        {
            guild = g;
            _client = c;
            if (weedStarted == false)
            {
                weedStarted = true;
                IConfiguration config = new ConfigurationBuilder()
                                        .AddJsonFile("appsettings.json", true, true)
                                        .Build();
                ulong voiceID = ulong.Parse(config["WeedChannelId"]);
                IVoiceChannel vc = await guild.GetVoiceChannelAsync(voiceID);
                await this.ScheduleWeed(guild, vc, config["FfmpegCommand"]);
            }
        }

        public async Task JoinAudio(IGuild guild, IVoiceChannel target)
        {
            //Tuple<IAudioClient, IVoiceChannel> client;
            //if (ConnectedChannels.TryGetValue(guild.Id, out client))
            //{
            //    await LeaveAudio(guild);
            //}
            if (target.Guild.Id != guild.Id)
            {
                return;
            }

            await LeaveAudio(guild);
            await Task.Delay(100);
            var audioClient = await target.ConnectAsync();

            if (ConnectedChannels.TryAdd(guild.Id, new Tuple<IAudioClient, IVoiceChannel>(audioClient, target)))
            {
                // If you add a method to log happenings from this service,
                // you can uncomment these commented lines to make use of that.
                //await Log(LogSeverity.Info, $"Connected to voice on {guild.Name}.");
            }
        }

        public async Task LeaveAudio(IGuild guild)
        {
            Tuple<IAudioClient, IVoiceChannel> client;
            if (ConnectedChannels.TryRemove(guild.Id, out client))
            {
                await client.Item1.StopAsync();
                //await Log(LogSeverity.Info, $"Disconnected from voice on {guild.Name}.");
            }
        }

        public async Task SendAudioAsync(IGuild guild, string path, string command)
        {
            // Your task: Get a full path to the file if the value of 'path' is only a filename.
            Tuple<IAudioClient, IVoiceChannel> client;
            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                //await Log(LogSeverity.Debug, $"Starting playback of {path} in {guild.Name}");
                playing = true;
                using (var ffmpeg = CreateProcess(path, command))
                using (var stream = client.Item1.CreatePCMStream(AudioApplication.Music))
                {
                    try { await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream); }
                    finally { await stream.FlushAsync(); stream.Close(); ffmpeg.Kill(true); playing = false; await PlayNextInQueue(guild, command); }
                }
            }
        }

        public async Task QueueLocalSong(IGuild guild, string path, ulong channelId, string command)
        {
            if (weedPlaying)
            {
                backupQueue.Enqueue(new AudioItem() { Path = path, PlayChannelId = channelId, AudioSource = AudioType.Local });
            }
            else
            {
                songQueue.Enqueue(new AudioItem() { Path = path, PlayChannelId = channelId, AudioSource = AudioType.Local });
                if (!playing)
                {
                    //want to trigger playing next song in queue
                    await PlayNextInQueue(guild, command);
                }
            }
        }

        public async Task QueueStreamedSong(IGuild guild, string path, ulong channelId, string command)
        {
            //download the youtube video from the URL
            string tempSongFilename = await DownloadYoutubeVideoAsync(path);

            //queue the downloaded file as normal
            if (weedPlaying)
            {
                backupQueue.Enqueue(new AudioItem() { Path = tempSongFilename, PlayChannelId = channelId, AudioSource = AudioType.YouTube });
            }
            else
            {
                songQueue.Enqueue(new AudioItem() { Path = tempSongFilename, PlayChannelId = channelId, AudioSource = AudioType.YouTube });

                if (!playing)
                {
                    //want to trigger playing next song in queue
                    await PlayNextInQueue(guild, command);
                }
            }
        }

        public async Task QueueTempSong(IGuild guild, IReadOnlyCollection<Attachment> attachments, ulong channelId, string command)
        {
            List<string> files = AttachmentHelper.DownloadAttachments(attachments, "assets/temp");
            string path = files[0];
            if (weedPlaying)
            {
                backupQueue.Enqueue(new AudioItem() { Path = path, PlayChannelId = channelId, AudioSource = AudioType.Local });
            }
            else
            {
                songQueue.Enqueue(new AudioItem() { Path = path, PlayChannelId = channelId, AudioSource = AudioType.Local });
                if (!playing)
                {
                    //want to trigger playing next song in queue
                    await PlayNextInQueue(guild, command);
                }
            }
        }

        public async Task PlayNextInQueue(IGuild guild, string command)
        {
            AudioItem nextInQueue;
            if (songQueue.TryDequeue(out nextInQueue))
            {
                IVoiceChannel channel = await guild.GetVoiceChannelAsync(nextInQueue.PlayChannelId);
                await JoinAudio(guild, channel);
                if (_client != null)
                {
                    string path = nextInQueue.Path;
                    //I fucking hate windows for making me do this bullshit
                    if (path.Contains("/temp/") || path.Contains("\\temp\\") || path.Contains("\\temp/") || path.Contains("/temp\\"))
                    {
                        await _client.SetGameAsync("Someone's mp3 file");
                    }
                    else
                    {
                        await _client.SetGameAsync(Path.GetFileName(nextInQueue.Path));
                    }
                }
                switch (nextInQueue.AudioSource)
                {
                    case AudioType.Local:
                        await SendAudioAsync(guild, nextInQueue.Path, command);
                        break;

                    case AudioType.YouTube:
                        await SendAudioAsync(guild, nextInQueue.Path, command);
                        break;

                    default:
                        throw new ArgumentException("Unknown audio type/source.");
                }
            }
            else
            {
                await LeaveAudio(guild);
                if (_client != null)
                {
                    await _client.SetGameAsync(null);
                }
                // Queue is empty, delete all .mp3 files in the assets/temp folder
                AttachmentHelper.DeleteFiles(AttachmentHelper.GetTempAssets("*.mp3"));
                AttachmentHelper.DeleteFiles(AttachmentHelper.GetTempAssets("*.mp4"));
            }
        }

        public async Task StopAllAudio(IGuild guild)
        {
            songQueue = new ConcurrentQueue<AudioItem>();
            playing = false;
            await LeaveAudio(guild);
            if (_client != null)
            {
                await _client.SetGameAsync(null);
            }
            //probably should do this, but we would have to figure out a way to wait til the ffmpeg process dies, which I don't want to do
            //the files will get wiped out eventually I bet
            //AttachmentHelper.DeleteFiles(AttachmentHelper.GetTempAssets("*.mp3"));
        }

        private Process CreateProcess(string path, string command)
        {
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
            backupQueue = songQueue;
            weedPlaying = true;
            await StopAllAudio(guild);
            await JoinAudio(guild, channel);
            string path = "assets/weedlmao.mp3";
            path = Path.GetFullPath(path);
            if (_client != null)
            {
                await _client.SetGameAsync("weeeeed");
            }
            await SendAudioAsync(guild, path, command);
            await LeaveAudio(guild);
            weedPlaying = false;
            songQueue = backupQueue;
            backupQueue = new ConcurrentQueue<AudioItem>();
            if (_client != null)
            {
                await _client.SetGameAsync(null);
            }
            PlayNextInQueue(guild, command);
            ScheduleWeed(guild, channel, command);
        }

        private async Task<string> DownloadYoutubeVideoAsync(string url)
        {
            string tempPath = Path.Combine(Environment.CurrentDirectory, "assets", "temp");
            string videoDataFilename = string.Empty;
            
            try
            {
                //download the youtube video data (mp4 format)
                var youtube = YouTube.Default;
                var video = await youtube.GetVideoAsync(url);
                var videoData = await video.GetBytesAsync();
                
                //write the mp4 file to the temp assets dir
                videoDataFilename = Path.Combine(tempPath, video.FullName);
                File.WriteAllBytes(videoDataFilename, videoData);
                return videoDataFilename;
                
                /*
                //convert the youtube video to mp3 format
                string outputFilename = Path.Combine(tempPath, $"{video.FullName}.mp3");
                var inputFile = new MediaFile { Filename = videoDataFilename };
                var outputFile = new MediaFile { Filename = outputFilename };
                using (var engine = new Engine("/bin/ffmpeg"))
                {
                    engine.GetMetadata(inputFile);
                    engine.Convert(inputFile, outputFile);
                }
                
                return outputFilename;
                */
            }
            finally
            {
                /*
                if (File.Exists(videoDataFilename))
                {
                    //remove the temp-downloaded video file
                    File.Delete(videoDataFilename);
                }
                */
            }
        }
    }
}
