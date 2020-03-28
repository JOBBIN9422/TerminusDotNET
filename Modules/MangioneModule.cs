using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using TerminusDotNetCore.Services;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.FileExtensions;

namespace TerminusDotNetCore.Modules
{
    public class MangioneModule : ServiceControlModule
    {
        private AudioService _service;
        private IConfiguration _config;

        public MangioneModule(IConfiguration config, AudioService service) : base(config)
        {
            _service = service;
            _service.ParentModule = this;
            _config = new ConfigurationBuilder()
                          .AddJsonFile("appsettings.json", true, true)
                          .Build();
        }

        private async Task<IReadOnlyCollection<Attachment>> GetAttachmentsAsync()
        {
            var attachments = Context.Message.Attachments;
            if (attachments == null || attachments.Count == 0)
            {
                //check the last 20 messages for attachments (from most recent to oldest)
                var messages = await Context.Channel.GetMessagesAsync(20).FlattenAsync();
                foreach (var message in messages)
                {
                    if (message.Attachments.Count == 1)
                    {
                        if ( message.Attachments.GetEnumerator().Current.Filename.EndsWith(".mp3") )
                        {
                            return (IReadOnlyCollection<Attachment>)message.Attachments ;
                        }
                    }
                }
                //if none of the previous messages had any attachments
                throw new NullReferenceException("Exactly one mp3 attachment must be present in one of the recent messages.");
            }
            else
            {
                return attachments;
            }
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Play a song of your choice in an audio channel of your choice (defaults to verbal shitposting)\nAvailable song aliases are: \"mangione1\", \"mangione2\", \"poloski\"")]
        public async Task PlaySong([Summary("name of song to play (use \"attached\" to play an attached mp3 file")]string song, [Summary("ID of channel to play in (defaults to verbal shitposting)")]string channelID = "-1")
        {
            if( Context != null && Context.Guild != null)
            {
                _service.SetGuildClient(Context.Guild, Context.Client);
            }
            // TODO allow this function to accept mp3 attachments and play those
            //check if channel id is valid and exists
            ulong voiceID;
            if ( channelID.Equals("-1") )
            {
                voiceID = ulong.Parse(_config["AudioChannelId"]);
            }
            else
            {
                try
                {
                    voiceID = ulong.Parse(channelID);
                }
                catch
                {
                    await ReplyAsync("Unable to parse channel ID, try letting it use the default");
                    return;
                }
            }
            if ( Context.Guild.GetVoiceChannel(voiceID) == null )
            {
                await ReplyAsync("Invalid channel ID, try letting it use the default");
                return;
            }
            //check if path is valid and exists
            // TODO check if file type can be played (mp3, wav, idk what ffmpeg can play)
            bool useFile = false;
            string path = _service.AudioPath;
            if ( song == "attached" )
            {
                useFile = true;
            }
            foreach ( string line in File.ReadAllLines(Path.Combine(_service.AudioPath, "audioaliases.txt")))
            {
                if ( line.StartsWith("#") || String.IsNullOrEmpty(line) )
                {
                    continue;
                }
                string[] tmp = line.Split(" ");
                if ( song.Equals(tmp[0]) )
                {
                    path = Path.Combine(path, tmp[1]);
                    break;
                }
            }
            if ( path.Equals(_service.AudioPath) )
            {
                path = Path.Combine(path, song);
            }
            if ( useFile )
            {
                IReadOnlyCollection<Attachment> atts = await GetAttachmentsAsync();
                await _service.QueueTempSong(Context.Guild, atts, voiceID, _config["FfmpegCommand"]);
            }
            else
            {
                path = Path.GetFullPath(path);
                if (!File.Exists(path))
                {
                    await ReplyAsync("File does not exist.");
                    Console.WriteLine(path);
                    return;
                }
                await _service.QueueLocalSong(Context.Guild, path, voiceID, _config["FfmpegCommand"]);
            }
        }

        [Command("search", RunMode = RunMode.Async)]
        public async Task SearchSong(string searchTerm, string channelID = "-1")
        {
            if (Context != null && Context.Guild != null)
            {
                _service.SetGuildClient(Context.Guild, Context.Client);
            }

            //check if channel id is valid and exists
            ulong voiceID;
            if (channelID.Equals("-1"))
            {
                voiceID = ulong.Parse(_config["AudioChannelId"]);
            }
            else
            {
                try
                {
                    voiceID = ulong.Parse(channelID);
                }
                catch
                {
                    await ReplyAsync("Unable to parse channel ID, try letting it use the default");
                    return;
                }
            }
            if (Context.Guild.GetVoiceChannel(voiceID) == null)
            {
                await ReplyAsync("Invalid channel ID, try letting it use the default");
                return;
            }

            await _service.QueueSearchedYoutubeSong(Context.Guild, searchTerm, voiceID, _config["FfmpegCommand"]);
        }

        [Command("playlist", RunMode = RunMode.Async)]
        public async Task AddPlaylist(string playlistUrl, string channelID = "-1")
        {
            if (Context != null && Context.Guild != null)
            {
                _service.SetGuildClient(Context.Guild, Context.Client);
            }

            //check if channel id is valid and exists
            ulong voiceID;
            if (channelID.Equals("-1"))
            {
                voiceID = ulong.Parse(_config["AudioChannelId"]);
            }
            else
            {
                try
                {
                    voiceID = ulong.Parse(channelID);
                }
                catch
                {
                    await ReplyAsync("Unable to parse channel ID, try letting it use the default");
                    return;
                }
            }
            if (Context.Guild.GetVoiceChannel(voiceID) == null)
            {
                await ReplyAsync("Invalid channel ID, try letting it use the default");
                return;
            }

            await _service.QueueYoutubePlaylist(Context.Guild, playlistUrl, voiceID, _config["FfmpegCommand"]);
        }

        [Command("yt", RunMode = RunMode.Async)]
        public async Task StreamSong(string url, string channelID = "-1")
        {
            if (Context != null && Context.Guild != null)
            {
                _service.SetGuildClient(Context.Guild, Context.Client);
            }
            
            //check if channel id is valid and exists
            ulong voiceID;
            if (channelID.Equals("-1"))
            {
                voiceID = ulong.Parse(_config["AudioChannelId"]);
            }
            else
            {
                try
                {
                    voiceID = ulong.Parse(channelID);
                }
                catch
                {
                    await ReplyAsync("Unable to parse channel ID, try letting it use the default");
                    return;
                }
            }
            if (Context.Guild.GetVoiceChannel(voiceID) == null)
            {
                await ReplyAsync("Invalid channel ID, try letting it use the default");
                return;
            }

            await _service.QueueYoutubeSongPreDownloaded(Context.Guild, url, voiceID, _config["FfmpegCommand"]);
        }

        [Command("playnext", RunMode = RunMode.Async)]
        [Summary("Play the next item in the song queue, if any.")]
        public async Task PlayNext()
        {
            await _service.PlayNextInQueue(Context.Guild, _config["FfmpegCommand"]);
        }
        
        [Command("songs", RunMode = RunMode.Async)]
        [Summary("List the contents of the song queue, if any.")]
        public async Task ListSongs()
        {
            List<Embed> songsList = _service.ListQueueContents();

            foreach (Embed embed in songsList)
            {
                await ReplyAsync(embed: embed);
            }
        }

        [Command("killmusic", RunMode = RunMode.Async)]
        [Summary("Flush song queue and leave voice channels")]
        public async Task KillMusic()
        {
            await _service.StopAllAudio(Context.Guild);
        }

        [Command("addpersistentsong", RunMode = RunMode.Async)]
        [Summary("Store an mp3 file to the server and give an alias for ease of use in !play commands")]
        public async Task AddSong([Summary("alias to use when playing this song in the future")]string alias)
        {
            IReadOnlyCollection<Attachment> atts = await GetAttachmentsAsync();
            _service.SaveSong(alias, atts);
        }

        [Command("availablesongs", RunMode = RunMode.Async)]
        [Summary("Prints the list of song aliases that are available locally")]
        public async Task PrintAvailableSongs()
        {
            List<Embed> aliasList = _service.ListAvailableAliases();

            foreach (Embed embed in aliasList)
            {
                await ReplyAsync(embed: embed);
            }
        }

    }
}
