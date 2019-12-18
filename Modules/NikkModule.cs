using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Modules
{
    public class NikkModule : TextModule
    {
        public NikkModule(Random random) : base(random) {}
        
        [Command("nikk")]
        [Summary("Responds with a quote from the legendary mercenary koder, Nikk Hemp(weed)street.")]
        public override async Task SayAsync()
        {
            var nikkPastas = File.ReadAllLines(Path.Combine("RandomMessages", "nikk.txt"));
            await ReplyAsync(nikkPastas[_random.Next(nikkPastas.Length)]);
        }
    }
}
