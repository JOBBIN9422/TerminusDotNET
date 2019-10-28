using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Modules
{
    public class TerminusModule : ModuleBase<SocketCommandContext>, ITextModule
    {
        [Command("terminus")]
        [Summary("Have a chat with Terminus :)")]
        public async Task SayAsync()
        {
            var random = new Random();
            var terminusPastas = File.ReadAllLines(@"RandomMessages\terminus.txt");
            await ReplyAsync(terminusPastas[random.Next(terminusPastas.Length)]);
        }
    }
}
