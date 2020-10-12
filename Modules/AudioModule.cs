using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using TerminusDotNetCore.Services;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using TerminusDotNetCore.Helpers;
using Discord.WebSocket;

namespace TerminusDotNetCore.Modules
{
    public class AudioModule : ServiceControlModule
    {
        private AudioService _service;

        public AudioModule(IConfiguration config, AudioService service) : base(config)
        {
            //do not need to set service config here - passed into audioSvc constructor via DI
            _service = service;
            _service.ParentModule = this;
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
        [Summary("Play a song of your choice in an audio channel of your choice (defaults to verbal shitposting). List local songs with !availablesongs.")]
        public async Task PlaySong([Summary("name of song to play (use \"attached\" to play an attached mp3 file")]string song, 
            [Summary("Which end of the queue to insert the song at (appended to the back by default.)")]string qEnd = "back", 
            [Summary("ID of channel to play in (defaults to verbal shitposting)")]string channelID = "-1")
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
                voiceID = ulong.Parse(Config["AudioChannelId"]);
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
                await _service.QueueTempSong(Context.Message.Author, atts, voiceID, qEnd != "front");
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
                await _service.QueueLocalSong(Context.Message.Author, path, voiceID, qEnd != "front");
            }
        }

        [Command("search", RunMode = RunMode.Async)]
        [Summary("Search for a YouTube video and add the result to the queue.")]
        public async Task SearchSong([Summary("YouTube search term (enclose in quotes if it contains spaces).")]string searchTerm, 
            [Summary("Channel ID to play the song in.")]string channelID = "-1")
        {
            if (Context != null && Context.Guild != null)
            {
                _service.SetGuildClient(Context.Guild, Context.Client);
            }

            //check if channel id is valid and exists
            ulong voiceID;
            if (channelID.Equals("-1"))
            {
                voiceID = ulong.Parse(Config["AudioChannelId"]);
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

            await _service.QueueSearchedYoutubeSong(Context.Message.Author, searchTerm, voiceID);
        }

        [Command("playlist", RunMode = RunMode.Async)]
        [Summary("Add all of the songs in the playlist to the queue in the order they appear in the playlist.")]
        public async Task AddPlaylist([Summary("The URL of the YouTube playlist to add.")]string playlistUrl, 
            [Summary("Which end of the queue to insert the song at (appended to the back by default.)")]string qEnd = "back", 
            [Summary("Channel ID to play the song in.")]string channelID = "-1")
        {
            if (Context != null && Context.Guild != null)
            {
                _service.SetGuildClient(Context.Guild, Context.Client);
            }

            //check if channel id is valid and exists
            ulong voiceID;
            if (channelID.Equals("-1"))
            {
                voiceID = ulong.Parse(Config["AudioChannelId"]);
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

            await _service.QueueYoutubePlaylist(Context.Message.Author, playlistUrl, voiceID, qEnd != "front");
        }

        [Command("yt", RunMode = RunMode.Async)]
        [Summary("Add the given YouTube video to the queue.")]
        public async Task StreamSong([Summary("URL of the YouTube video to add.")]string url, 
            [Summary("Which end of the queue to insert the song at (appended to the back by default.)")]string qEnd = "back",
            [Summary("Channel ID to play the song in.")]string channelID = "-1")
        {
            if (Context != null && Context.Guild != null)
            {
                _service.SetGuildClient(Context.Guild, Context.Client);
            }
            
            //check if channel id is valid and exists
            ulong voiceID;
            if (channelID.Equals("-1"))
            {
                voiceID = ulong.Parse(Config["AudioChannelId"]);
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

            await _service.QueueYoutubeSongPreDownloaded(Context.Message.Author, url, voiceID, qEnd != "front");
        }

        [Command("playnext", RunMode = RunMode.Async)]
        [Summary("Play the next item in the song queue, if any.")]
        public async Task PlayNext()
        {
            //don't need to call playnext since it recursively calls itself after playback

            await Logger.Log(new LogMessage(LogSeverity.Info, "AudioSvc", $"User '{Context.Message.Author.Username}' requested a playnext."));
            _service.StopFfmpeg();
        }

        [Command("qfront", RunMode = RunMode.Async)]
        [Summary("Move the item at the given index to the front of the queue.")]
        public async Task MoveSongToFront([Summary("the index of the song to move to the front (1-indexed based on the items in the `!songs` list).")]int index = -1)
        {
            if (index == -1)
            {
                await ReplyAsync("Please provide the index of a song in the queue (!songs).");
                return;
            }

            await _service.MoveSongToFront(index);
        }

        [Command("playing", RunMode = RunMode.Async)]
        [Summary("Display info about the currently playing song, if any.")]
        public async Task DisplayCurrentSong()
        {
            Embed currSongInfo = await _service.DisplayCurrentSong();
            if (currSongInfo != null)
            {
                await ReplyAsync(embed: currSongInfo);
            }

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
            await _service.StopAllAudio();
        }

        [Command("addlocalsong", RunMode = RunMode.Async)]
        [Summary("Store an audio file on the server and give an alias for use in !play commands.")]
        public async Task AddSong([Summary("alias to use when playing this song in the future")]string alias)
        {
            IReadOnlyCollection<Attachment> atts = await GetAttachmentsAsync();
            _service.SaveSong(alias, atts);
        }

        [Command("alias", RunMode = RunMode.Async)]
        [Summary("Store the currently-playing song to the server and give an alias for use in !play commands.")]
        public async Task AddCurrentSong([Summary("alias to use when playing this song in the future")]string alias)
        {
            await _service.SaveCurrentSong(alias);
        }

        [Command("localsongs", RunMode = RunMode.Async)]
        [Summary("Prints the list of song aliases that are available locally.")]
        public async Task PrintAvailableSongs()
        {
            List<Embed> aliasList = _service.ListAvailableAliases();

            foreach (Embed embed in aliasList)
            {
                await ReplyAsync(embed: embed);
            }
        }

        [Command("qsave", RunMode = RunMode.Async)]
        [Summary("Saves the queue contents (if any) to file.")]
        public async Task SaveQueueContents()
        {
            if (Context != null && Context.Guild != null)
            {
                _service.SetGuildClient(Context.Guild, Context.Client);
            }

            await _service.SaveQueueContents();
        }

        [Command("qload", RunMode = RunMode.Async)]
        [Summary("Loads the queue contents (if any) from file.")]
        public async Task LoadQueueContents()
        {
            if (Context != null && Context.Guild != null)
            {
                _service.SetGuildClient(Context.Guild, Context.Client);
            }

            await _service.LoadQueueContents();
        }

        [Command("weed", RunMode = RunMode.Async)]
        public async Task ForceWeed()
        {
            if (Context != null && Context.Guild != null)
            {
                _service.SetGuildClient(Context.Guild, Context.Client);
            }

            await _service.PlayWeed();
        }

        [Group("hideki")]
        public class HidekiAudioModule : ServiceControlModule
        {
            private AudioService _service;
            public HidekiAudioModule(IConfiguration config, AudioService service) : base(config)
            {
                //do not need to set service config here - passed into audioSvc constructor via DI
                _service = service;
                _service.ParentModule = this;
            }

            [Command]
            [Summary("Display usage info about the `hideki` command.")]
            public async Task PrintUsageInfo()
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.Title = "**_UNDERSTAND UNDERSTAND_**";
                CommandSummaryHelper.GenerateGroupCommandSummary(GetType(), embedBuilder, "hideki");
                CommandSummaryHelper.GenerateGroupCommandSummary(typeof(TwitterModule.HidekiTwitterModule), embedBuilder, "hideki");
                await ReplyAsync(embed: embedBuilder.Build());
            }

            [Command("jam", RunMode = RunMode.Async)]
            [Summary("Add a random Hideki Naganuma song to the queue.")]
            public async Task AddRandomHidekiSong(
            [Summary("Which end of the queue to insert the song at (appended to the back by default.)")]string qEnd = "back",
            [Summary("Channel ID to play the song in.")]string channelID = "-1")
            {
                if (Context != null && Context.Guild != null)
                {
                    _service.SetGuildClient(Context.Guild, Context.Client);
                }
                ulong voiceID;
                if (channelID.Equals("-1"))
                {
                    voiceID = ulong.Parse(Config["AudioChannelId"]);
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

                await _service.AddRandomHidekiSong(Context.Message.Author, voiceID, qEnd != "front");
            }
        }

        [Group("radio")]
        public class RadioAudioModule : ServiceControlModule
        {
            private AudioService _service;
            public RadioAudioModule(IConfiguration config, AudioService service) : base(config)
            {
                //do not need to set service config here - passed into audioSvc constructor via DI
                _service = service;
                _service.ParentModule = this;
            }

            [Command]
            [Summary("Display usage info about the `radio` command.")]
            public async Task PrintUsageInfo()
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                CommandSummaryHelper.GenerateGroupCommandSummary(GetType(), embedBuilder, "radio");
                await ReplyAsync(embed: embedBuilder.Build());
            }

            [Command("create", RunMode = RunMode.Async)]
            public async Task CreatePlaylist(string name)
            {
                if (Context != null && Context.Guild != null)
                {
                    _service.SetGuildClient(Context.Guild, Context.Client);
                }

                if (string.IsNullOrEmpty(name))
                {
                    await ReplyAsync("Please provide a playlist name.");
                    return;
                }

                await _service.CreateRadioPlaylist(Context.Message.Author, name);
            }

            [Command("add", RunMode = RunMode.Async)]
            public async Task AddSongToPlaylist(string playlistName, string url)
            {
                if (Context != null && Context.Guild != null)
                {
                    _service.SetGuildClient(Context.Guild, Context.Client);
                }

                if (string.IsNullOrEmpty(playlistName))
                {
                    await ReplyAsync("Please provide a playlist name.");
                    return;
                }
                
                if (string.IsNullOrEmpty(url))
                {
                    await ReplyAsync("Please provide a YouTube URL.");
                    return;
                }

                await _service.AddRadioSong(Context.Message.Author, playlistName, url);
            }

            [Command("delete", RunMode = RunMode.Async)]
            public async Task RemoveSongFromPlaylist(string playlistName, int index = -1)
            {
                if (Context != null && Context.Guild != null)
                {
                    _service.SetGuildClient(Context.Guild, Context.Client);
                }

                if (string.IsNullOrEmpty(playlistName))
                {
                    await ReplyAsync("Please provide a playlist name.");
                    return;
                }

                if (index == -1)
                {
                    //delete the playlist if no song specified
                    await _service.DeleteRadioPlaylist(Context.Message.Author, playlistName);
                    return;
                }

                await _service.DeleteRadioSong(Context.Message.Author, playlistName, index);
            }

            [Command("whitelist", RunMode = RunMode.Async)]
            public async Task WhitelistUserForPlaylist(string playlistName, SocketUser whitelistUser = null)
            {
                if (Context != null && Context.Guild != null)
                {
                    _service.SetGuildClient(Context.Guild, Context.Client);
                }

                if (string.IsNullOrEmpty(playlistName))
                {
                    await ReplyAsync("Please provide a playlist name.");
                    return;
                }

                if (whitelistUser == null)
                {
                    await ReplyAsync("Please provide a `@user` to whitelist.");
                    return;
                }

                await _service.WhitelistUserForRadioPlaylist(playlistName, Context.Message.Author, whitelistUser);
            }

            [Command("blacklist", RunMode = RunMode.Async)]
            public async Task RemoveWhitelistUserFromPlaylist(string playlistName, SocketUser blacklistUser = null)
            {
                if (Context != null && Context.Guild != null)
                {
                    _service.SetGuildClient(Context.Guild, Context.Client);
                }

                if (string.IsNullOrEmpty(playlistName))
                {
                    await ReplyAsync("Please provide a playlist name.");
                    return;
                }

                if (blacklistUser == null)
                {
                    await ReplyAsync("Please provide a `@user` to remove from whitelist.");
                    return;
                }

                await _service.RemoveWhitelistUserFromRadioPlaylist(playlistName, Context.Message.Author, blacklistUser);
            }

            [Command("play", RunMode = RunMode.Async)]
            public async Task LoadPlaylist(string name, string shuffle = "")
            {


                if (Context != null && Context.Guild != null)
                {
                    _service.SetGuildClient(Context.Guild, Context.Client);
                }

                if (string.IsNullOrEmpty(name))
                {
                    await ReplyAsync("Please provide a playlist name.");
                    return;
                }

                //shuffle if requested
                await _service.LoadRadioPlaylist(Context.Message.Author, name, shuffle == "shuffle");
            }

            [Command("list", RunMode = RunMode.Async)]
            public async Task ShowPlaylistContents(string name)
            {
                if (Context != null && Context.Guild != null)
                {
                    _service.SetGuildClient(Context.Guild, Context.Client);
                }

                if (string.IsNullOrEmpty(name))
                {
                    await ReplyAsync("Please provide a playlist name.");
                    return;
                }

                await _service.ShowRadioPlaylistContents(name);
            }

            [Command("playlists", RunMode = RunMode.Async)]
            public async Task ShowAllPlaylists()
            {
                if (Context != null && Context.Guild != null)
                {
                    _service.SetGuildClient(Context.Guild, Context.Client);
                }
                await _service.ShowAllRadioPlaylists();
            }
        }
    }
}
