using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace TerminusDotNetCore.Modules
{
    public class ChaseModule : TextModule
    {
        public ChaseModule(Random random) : base(random) {}
        
        [Command("chase")]
        [Summary("Responds with a random quote from the legendary mercenary programmer, Solid Chase.")]
        public override async Task SayAsync()
        {
            var chasePastas = File.ReadAllLines(Path.Combine("RandomMessages", "chase.txt"));
            await ReplyAsync(chasePastas[_random.Next(chasePastas.Length)]);
        }
    }
}
