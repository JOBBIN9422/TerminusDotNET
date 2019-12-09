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

namespace TerminusDotNetCore.Services
{
    public class AudioService : ICustomService
    {
        public IServiceModule ParentModule { get; set; }
        private ConcurrentQueue<Tuple<string,ulong>> songQueue = new ConcurrentQueue<Tuple<string, ulong>>();
        private ConcurrentQueue<Tuple<string,ulong>> backupQueue = new ConcurrentQueue<Tuple<string, ulong>>();
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
            if(weedStarted == false)
            {
                weedStarted = true;
                IConfiguration config = new ConfigurationBuilder()
                                        .AddJsonFile("appsettings.json", true, true)
                                        .Build();
                ulong voiceID = ulong.Parse(config["WeedChannelId"]);
                IVoiceChannel vc = await guild.GetVoiceChannelAsync(voiceID);
                await this.ScheduleWeed(guild, vc , config["FfmpegCommand"]);
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
                    finally { await stream.FlushAsync(); stream.Close(); ffmpeg.Kill(true); playing = false; await PlayNextInQueue(guild,command); }
                }
            }
        }

        public async Task QueueSong(IGuild guild, string path, ulong channelId, string command)
        {
            if ( weedPlaying )
            {
                backupQueue.Enqueue(new Tuple<string,ulong>(path, channelId));
            }
            else
            {
                songQueue.Enqueue(new Tuple<string,ulong>(path, channelId));
                if( !playing ) 
                {
                    //want to trigger playing next song in queue
                    await PlayNextInQueue(guild, command);
                }
            }
        }

        public async Task PlayNextInQueue(IGuild guild, string command)
        {
            Tuple<string, ulong> nextInQueue;
            if (songQueue.TryDequeue(out nextInQueue))
            {
                IVoiceChannel channel = await guild.GetVoiceChannelAsync(nextInQueue.Item2);
                await JoinAudio(guild, channel);
                if ( _client != null )
                {
                    await _client.SetGameAsync(Path.GetFileName(nextInQueue.Item1));
                }
                await SendAudioAsync(guild, nextInQueue.Item1, command);
            }
            else
            {
                await LeaveAudio(guild);
                if ( _client != null )
                {
                    await _client.SetGameAsync(null);
                }
            }
        }

        public async Task StopAllAudio(IGuild guild)
        {
            songQueue = new ConcurrentQueue<Tuple<string, ulong>>();
            playing = false;
            await LeaveAudio(guild);
            if ( _client != null )
            {
                await _client.SetGameAsync(null);
            }
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
            if ( now > fourTwenty ) 
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
            if ( _client != null )
            {
                await _client.SetGameAsync("weeeeed");
            }
            await SendAudioAsync(guild, path, command);
            await LeaveAudio(guild);
            weedPlaying = false;
            songQueue = backupQueue;
            backupQueue = new ConcurrentQueue<Tuple<string, ulong>>();
            if ( _client != null )
            {
                await _client.SetGameAsync(null);
            }
            await PlayNextInQueue(guild, command);
            await ScheduleWeed(guild, channel, command);
        }

    }
}
