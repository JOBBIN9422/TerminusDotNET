using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Modules
{
    public class BotManagementModule : ModuleBase<SocketCommandContext>
    {
        private Bot _bot;

        public IConfiguration Config { get; set; }

        public BotManagementModule(IConfiguration config, Bot bot)
        {
            _bot = bot;
            Config = config;
        }

        [Command("die")]
        public void KillBot()
        {
            _bot.IsActive = false;
        }

        [Command("live")]
        public void ResurrectBot()
        {
            _bot.IsActive = true;
        }

        [Command("regex")]
        public async Task SetRegexState(string state = null)
        {
            if (string.IsNullOrEmpty(state))
            {
                await ReplyAsync("Please provide a state argument (e.g. on/off).");
            }
            state = state.ToLower();

            if (state == "off" || state == "n" || state == "no" || state == "disabled")
            {
                _bot.IsRegexActive = false;
            }
            else if (state == "on" || state == "y" || state == "yes" || state == "enabled")
            {
                _bot.IsRegexActive = true;
            }
            else
            {
                return;
            }
        }
    }
}
