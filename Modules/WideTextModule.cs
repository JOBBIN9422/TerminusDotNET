using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using TerminusDotNetCore.Services;

namespace TerminusDotNetCore.Modules
{
    public class WideTextModule : ModuleBase<SocketCommandContext>, IServiceModule
    {
        private WideTextService _wideTextService;

        public WideTextModule(WideTextService service)
        {
            _wideTextService = service;
            _wideTextService.ParentModule = this;
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

        [Command("wide", RunMode = RunMode.Async)]
        [Summary("Converts the input message (or the contents of the previous message) into full-width text.")]
        public async Task ConvertMessageToFullWidthAsync([Summary("the message to convert")][Remainder]string message = null)
        {
            string wideText = string.Empty;
            if (string.IsNullOrEmpty(message))
            {
                //check if the previous message has any text
                var messages = await Context.Channel.GetMessagesAsync(2).FlattenAsync();
                var priorMessage = messages.Last();
                if (!string.IsNullOrEmpty(priorMessage.Content))
                {
                    wideText = _wideTextService.ConvertMessage(priorMessage.Content);
                }
            }
            else 
            {
                wideText = _wideTextService.ConvertMessage(message);
            }
            await ReplyAsync(wideText);
        }
    }
}
