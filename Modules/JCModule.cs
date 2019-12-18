using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Modules
{
    public class JCModule : TextModule
    {
        public JCModule(Random random) : base(random) {}
        
        [Command("JC")]
        [Summary("Responds with a random JC Denton quote.")]
        public override async Task SayAsync()
        {
            var jcPastas = File.ReadAllLines(Path.Combine("RandomMessages", "jc.txt"));
            await ReplyAsync(jcPastas[_random.Next(jcPastas.Length)]);
        }
    }
}
