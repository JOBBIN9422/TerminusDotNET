using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace TerminusDotNetConsoleApp.Modules
{
    class ChaseModule : ModuleBase<SocketCommandContext>
    {
        
        [Command("chase")]
        [Summary("Echoes a spicy chase quote.")]
        public async Task SayAsync()
        {
            var random = new Random();
            var nikkPastas = File.ReadAllLines(@"RandomMessages\chase.txt");
            await Context.Channel.SendMessageAsync(nikkPastas[random.Next(nikkPastas.Length)]);
        }
    }
}
