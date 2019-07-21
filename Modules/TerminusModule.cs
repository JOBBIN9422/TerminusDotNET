using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TerminusDotNetConsoleApp.Modules
{
    public class TerminusModule : ModuleBase<SocketCommandContext>
    {
        [Command("terminus")]
        [Summary("Echoes a spicy terminus quote.")]
        public async Task SayAsync()
        {
            var random = new Random();
            var terminusPastas = File.ReadAllLines(@"RandomMessages\terminus.txt");
            await ReplyAsync(terminusPastas[random.Next(terminusPastas.Length)]);
        }
    }
}
