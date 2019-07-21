using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TerminusDotNetConsoleApp.Modules
{
    public class NikkModule : ModuleBase<SocketCommandContext>, ITextModule
    {
        [Command("nikk")]
        [Summary("Echoes a spicy nikk quote.")]
        public async Task SayAsync()
        {
            var random = new Random();
            var nikkPastas = File.ReadAllLines(@"RandomMessages\nikk.txt");
            await ReplyAsync(nikkPastas[random.Next(nikkPastas.Length)]);
        }
    }
}
