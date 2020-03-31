using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Modules
{
    public class TerminusModule : TextModule
    {
        public TerminusModule(Random random) : base(random) {}
    
        [Command("terminus")]
        [Summary("Have a chat with Terminus :)")]
        public override async Task SayAsync()
        {
            var terminusPastas = File.ReadAllLines(Path.Combine("RandomMessages", "terminus.txt"));
            await ReplyAsync(terminusPastas[_random.Next(terminusPastas.Length)]);
        }
    }
}
