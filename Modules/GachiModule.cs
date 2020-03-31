using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Modules
{
    public class GachiModule : TextModule
    {
        public GachiModule(Random random) : base(random) {}
        
        [Command("gachi")]
        [Summary("Responds with a random gachimuchi quote.")]
        public override async Task SayAsync()
        {
            var gachiPastas = File.ReadAllLines(Path.Combine("RandomMessages", "gachi.txt"));
            await ReplyAsync(gachiPastas[_random.Next(gachiPastas.Length)]);
        }
    }
}
