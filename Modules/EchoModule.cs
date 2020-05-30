using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Modules
{
    public class EchoModule : ModuleBase<SocketCommandContext>
    {
        [Command("echo")]
        [Summary("Responds with the given text.")]
        public async Task Echo([Summary("The text to be echoed.")][Remainder] string text)
        {
            await ReplyAsync(text);
        }
    }
}
