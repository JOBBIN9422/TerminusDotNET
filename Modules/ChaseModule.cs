using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace TerminusDotNetConsoleApp.Modules
{
    public class ChaseModule : ModuleBase<SocketCommandContext>, ITextModule
    {
        [Command("chase")]
        [Summary("Echoes a spicy chase quote.")]
        public async Task SayAsync()
        {
            var random = new Random();
            var chasePastas = File.ReadAllLines(@"RandomMessages\chase.txt");
            await ReplyAsync(chasePastas[random.Next(chasePastas.Length)]);
        }
    }
}
