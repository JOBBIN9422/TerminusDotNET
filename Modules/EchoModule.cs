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

        public async Task Echo([Remainder] string text)
        {
            await ReplyAsync(text);
        }
    }
}
