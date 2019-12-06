using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using TerminusDotNetCore.Services;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.FileExtensions;

namespace TerminusDotNetCore.Modules
{
    public class MangioneModule : ModuleBase<SocketCommandContext>, IServiceModule
    {
        private AudioService _service;

        public MangioneModule(AudioService service)
        {
            _service = service;
            _service.ParentModule = this;
        }

        public async Task ServiceReplyAsync(string s, EmbedBuilder embedBuilder = null)
        {
            if (embedBuilder == null)
            {
                await ReplyAsync(s);
            }
            else
            {
                await ReplyAsync(s, false, embedBuilder.Build());
            }
        }

        [Command("mangione", RunMode = RunMode.Async)]
        [Summary("Play some chill beats in the verbal shitposting channel")]
        public async Task PlayChuckAsync()
        {
            string path = "assets/feels_so_good.mp3";
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
            {
                await ReplyAsync("File does not exist.");
                return;
            }
            IConfiguration config = new ConfigurationBuilder()
                                        .AddJsonFile("appsettings.json", true, true)
                                        .Build();
            ulong voiceID = ulong.Parse(config["AudioChannelId"]);
            await _service.JoinAudio(Context.Guild, Context.Guild.GetVoiceChannel(voiceID));
            await _service.SendAudioAsync(Context.Guild, path);
            await _service.LeaveAudio(Context.Guild);
        }
    }
}
