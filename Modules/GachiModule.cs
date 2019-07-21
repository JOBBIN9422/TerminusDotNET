using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TerminusDotNetConsoleApp.Modules
{
    public class GachiModule : ModuleBase<SocketCommandContext>
    {
        [Command("gachi")]
        [Summary("Echoes a spicy gachi quote.")]
        public async Task SayAsync()
        {
            var random = new Random();
            var gachiPastas = File.ReadAllLines(@"RandomMessages\gachi.txt");
            await ReplyAsync(gachiPastas[random.Next(gachiPastas.Length)]);
        }
    }
}
