using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Modules
{
    public class NikkModule : ModuleBase<SocketCommandContext>, ITextModule
    {
        [Command("nikk")]
        [Summary("Responds with a quote from the legendary mercenary koder, Nikk Hemp(weed)street.")]
        public async Task SayAsync()
        {
            var random = new Random();
            var nikkPastas = File.ReadAllLines(Path.Combine("RandomMessages", "nikk.txt"));
            await ReplyAsync(nikkPastas[random.Next(nikkPastas.Length)]);
        }
    }
}
