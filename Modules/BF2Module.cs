using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Modules
{
    public class BF2Module : TextModule
    {
        public BF2Module(Random random) : base(random) {}
        
        [Command("BF2")]
        [Summary("Star Wars Battlefront 2 Video Game Clone Trooper Quotes")]
        public override async Task SayAsync()
        {
            var bf2Pastas = File.ReadAllLines(Path.Combine("RandomMessages", "bf2.txt"));
            await ReplyAsync(bf2Pastas[_random.Next(bf2Pastas.Length)]);
        }
    }
}
