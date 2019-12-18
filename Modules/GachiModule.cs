using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Modules
{
    public class GachiModule : ModuleBase<SocketCommandContext>, ITextModule
    {
        [Command("gachi")]
        [Summary("Responds with a random gachimuchi quote.")]
        public async Task SayAsync()
        {
            var random = new Random();
            var gachiPastas = File.ReadAllLines(Path.Combine("RandomMessages", "gachi.txt"));
            await ReplyAsync(gachiPastas[random.Next(gachiPastas.Length)]);
        }
    }
}
