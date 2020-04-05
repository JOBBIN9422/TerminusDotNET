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

        [Command("regex")]
        public async Task SetRegexState(string state = null)
        {
            if (string.IsNullOrEmpty(state))
            {
                //treat the command as a query if no setting is provided
                string regexState = _bot.IsRegexActive ? "enabled" : "disabled";
                await ReplyAsync($"Regex responses are currently {regexState}.");
                return;
            }
            state = state.ToLower();

            if (state == "off" || state == "n" || state == "no" || state == "disabled")
            {
                _bot.IsRegexActive = false;
                await ReplyAsync("Disabled regex responses.");
            }
            else if (state == "on" || state == "y" || state == "yes" || state == "enabled")
            {
                _bot.IsRegexActive = true;
                await ReplyAsync("Enabled regex responses.");
            }
            else
            {
                return;
            }
        }
    }
}
