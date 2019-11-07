using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Modules
{
    public class JCModule : ModuleBase<SocketCommandContext>, ITextModule
    {
        [Command("JC")]
        [Summary("Responds with a random JC Denton quote.")]
        public async Task SayAsync()
        {
            var random = new Random();
            var jcPastas = File.ReadAllLines(Path.Combine("RandomMessages", "terminus.txt"));
            await ReplyAsync(jcPastas[random.Next(jcPastas.Length)]);
        }
    }
}
