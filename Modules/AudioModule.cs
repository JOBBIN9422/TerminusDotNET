using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using TerminusDotNetCore.Services;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using TerminusDotNetCore.Helpers;
using TerminusDotNetCore.NamedArgs;
using Discord.WebSocket;

namespace TerminusDotNetCore.Modules
{
    public class AudioModule : ServiceControlModule
    {
        private AudioService _service;

        private Dictionary<string, ulong> _channelNameToIdMap = new Dictionary<string, ulong>();

        public AudioModule(IConfiguration config, AudioService service) : base(config)
        {
            //do not need to set service config here - passed into audioSvc constructor via DI
            _service = service;
            _service.ParentModule = this;

            //create a mapping from channel names to their IDs
            _channelNameToIdMap.Add("main", ulong.Parse(config["AudioChannelId"]));
            _channelNameToIdMap.Add("weed", ulong.Parse(config["WeedChannelId"]));
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Play a song of your choice in an audio channel of your choice (defaults to verbal shitposting). List local songs with !availablesongs.")]
        public async Task PlaySong([Summary("name of song to play (use \"attached\" to play an attached mp3 file")]string song, AudioQueueArgs namedArgs)
        {
            //check if channel id is valid and exists
            if (!_channelNameToIdMap.ContainsKey(namedArgs.Channel))
            {
                await ReplyAsync("Invalid channel name (please use `main` or `weed`).");
                return;
            }
            ulong voiceID = _channelNameToIdMap[channelName];

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
                IReadOnlyCollection<Attachment> atts = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Media);
                if (atts == null)
                {
                    throw new NullReferenceException("No media attachments were found in the current or previous 20 messages.");
                }
                await _service.QueueTempSong(Context.Message.Author, atts, voiceID, namedArgs.Append);
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
            [Summary("Channel name to play the song in (`main` or `weed`).")]string channelName = "main")
        {
            //check if channel id is valid and exists
            if (!_channelNameToIdMap.ContainsKey(channelName))
            {
                await ReplyAsync("Invalid channel name (please use `main` or `weed`).");
                return;
            }
            ulong voiceID = _channelNameToIdMap[channelName];

            await _service.QueueSearchedYoutubeSong(Context.Message.Author, searchTerm, voiceID);
        }

        [Command("playlist", RunMode = RunMode.Async)]
        [Summary("Add all of the songs in the playlist to the queue in the order they appear in the playlist.")]
        public async Task AddPlaylist([Summary("The URL of the YouTube playlist to add.")]string playlistUrl, 
            [Summary("Which end of the queue to insert the song at (appended to the back by default.)")]string qEnd = "back",
            [Summary("whether or not to shuffle the playlist when adding it to the song queue.")]string shuffle = "false",
            [Summary("Channel name to play the song in (`main` or `weed`).")]string channelName = "main")
        {
            //check if channel id is valid and exists
            if (!_channelNameToIdMap.ContainsKey(channelName))
            {
                await ReplyAsync("Invalid channel name (please use `main` or `weed`).");
                return;
            }
            ulong voiceID = _channelNameToIdMap[channelName];

            await _service.QueueYoutubePlaylist(Context.Message.Author, playlistUrl, voiceID, qEnd != "front", shuffle == "shuffle");
        }

        [Command("downloader", RunMode = RunMode.Async)]
        [Summary("Switch the library used to download YouTube videos.")]
        public async Task SwitchYoutubeDownloaderLibrary([Summary("The library alias (`libvideo` or `yt-explode`).")]string libName = "check")
        {
            if (libName == "check")
            {
                string currLibName = _service.GetYoutubeDownloaderName();
                await ReplyAsync($"Currently using YouTube downloader library `{currLibName}`");
            }
            else
            {
                await _service.SwitchYoutubeDownloaderLibrary(libName);
            }
        }

        [Command("yt", RunMode = RunMode.Async)]
        [Summary("Add the given YouTube video to the queue.")]
        public async Task StreamSong([Summary("URL of the YouTube video to add.")]string url, 
            [Summary("Which end of the queue to insert the song at (appended to the back by default.)")]string qEnd = "back",
            [Summary("Channel name to play the song in (`main` or `weed`).")]string channelName = "main")
        {
            //check if channel id is valid and exists
            if (!_channelNameToIdMap.ContainsKey(channelName))
            {
                await ReplyAsync("Invalid channel name (please use `main` or `weed`).");
                return;
            }
            ulong voiceID = _channelNameToIdMap[channelName];

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
            IReadOnlyCollection<Attachment> atts = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Media);
            if (atts == null)
            {
                throw new NullReferenceException("No media attachments were found in the current or previous 20 messages.");
            }
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
            await _service.SaveQueueContents();
        }

        [Command("qload", RunMode = RunMode.Async)]
        [Summary("Loads the queue contents (if any) from file.")]
        public async Task LoadQueueContents()
        {
            await _service.LoadQueueContents();
        }

        [Command("weed", RunMode = RunMode.Async)]
        public async Task ForceWeed()
        {
            await _service.PlayWeed();
        }

        [Group("hideki")]
        public class HidekiAudioModule : ServiceControlModule
        {
            private AudioService _service;

            private Dictionary<string, ulong> _channelNameToIdMap = new Dictionary<string, ulong>();

            public HidekiAudioModule(IConfiguration config, AudioService service) : base(config)
            {
                //do not need to set service config here - passed into audioSvc constructor via DI
                _service = service;
                _service.ParentModule = this;

                //create a mapping from channel names to their IDs
                _channelNameToIdMap.Add("main", ulong.Parse(config["AudioChannelId"]));
                _channelNameToIdMap.Add("weed", ulong.Parse(config["WeedChannelId"]));
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
            [Summary("Channel name to play the song in (`main` or `weed`).")]string channelName = "main")
            {
                //check if channel id is valid and exists
                if (!_channelNameToIdMap.ContainsKey(channelName))
                {
                    await ReplyAsync("Invalid channel name (please use `main` or `weed`).");
                    return;
                }
                ulong voiceID = _channelNameToIdMap[channelName];

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
            [Summary("Create a new, empty playlist. The command issuer is the owner of this playlist.")]
            public async Task CreatePlaylist([Summary("Name of the new playlist.")]string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    await ReplyAsync("Please provide a playlist name.");
                    return;
                }

                await _service.CreateRadioPlaylist(Context.Message.Author, name);
            }

            [Command("add", RunMode = RunMode.Async)]
            [Summary("Add a YouTube song to the given playlist (if it exists).")]
            public async Task AddSongToPlaylist(
                [Summary("The name of the playlist to add a song to.")]string playlistName, 
                [Summary("URL of the YouTube song to add.")]string url)
            {
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
            [Summary("Delete a song from the given playlist by its index. If no index is provided, then delete the playlist. Only the playlist owner may delete a playlist. Only whitelisted users may delete a song from a playlist.")]
            public async Task RemoveSongFromPlaylist(
                [Summary("The name of the playlist to delete or delete a song from.")]string playlistName, 
                [Summary("The index of the song in the playlist to delete (obtain with `!radio list <playlist-name>` command). If not given, delete the entire playlist.")]int index = -1)
            {
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
            [Summary("Add a user to the whitelist of the given playlist. This user can then add and delete songs to/from the playlist.")]
            public async Task WhitelistUserForPlaylist(
                [Summary("The name of the playlist to whitelist a user for.")]string playlistName, 
                [Summary("The `@user` to add to the whitelist of the given playlist.")]SocketUser whitelistUser = null)
            {
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
            [Summary("Remove a user from the whitelist of the given playlist (if they are on the whitelist already).")]
            public async Task RemoveWhitelistUserFromPlaylist(
                [Summary("The name of the playlist to delete a user from.")]string playlistName, 
                [Summary("The `@user` to remove from the whitelist of the given playlist (if they are on the whitelist already).")]SocketUser blacklistUser = null)
            {
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
            [Summary("Load the specified playlist into the song queue.")]
            public async Task LoadPlaylist([Summary("The name of the playlist to start playing.")]string name, [Summary("Shuffle the playlist if this argument is not null/empty (e.g. `!radio play <name> shuffle`).")]string shuffle = "")
            {
                if (string.IsNullOrEmpty(name))
                {
                    await ReplyAsync("Please provide a playlist name.");
                    return;
                }

                //shuffle if requested
                await _service.LoadRadioPlaylist(Context.Message.Author, name, shuffle == "shuffle");
            }

            [Command("list", RunMode = RunMode.Async)]
            [Summary("List the contents and whitelist of the given playlist.")]
            public async Task ShowPlaylistContents([Summary("Name of the playlist to show details for.")]string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    await ReplyAsync("Please provide a playlist name.");
                    return;
                }

                await _service.ShowRadioPlaylistContents(name);
            }

            [Command("playlists", RunMode = RunMode.Async)]
            [Summary("List all existing playlists.")]
            public async Task ShowAllPlaylists()
            {
                await _service.ShowAllRadioPlaylists();
            }
        }
    }
}
