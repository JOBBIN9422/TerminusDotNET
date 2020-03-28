using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Services;

namespace TerminusDotNetCore.Modules
{
    public class ServiceControlModule : ModuleBase<SocketCommandContext>
    {
        //shared config object - passed via DI
        public IConfiguration Config { get; set; }

        //shared client secrets file object - passed via DI
        public IConfiguration ClientSecrets { get; set; }

        public ServiceControlModule(IConfiguration config, IConfiguration secrets)
        {
            Config = config;
            ClientSecrets = secrets;
        }


        //allow services to reply on a text channel
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
    }
}
