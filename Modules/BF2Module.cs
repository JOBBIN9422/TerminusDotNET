using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TerminusDotNetConsoleApp.Modules
{
    public class BF2Module : ModuleBase<SocketCommandContext>, ITextModule
    {
        [Command("BF2")]
        [Summary("Star Wars Battlefront 2 Video Game Clone Trooper Quotes")]
        public async Task SayAsync()
        {
            var random = new Random();
            var bf2Pastas = File.ReadAllLines(@"RandomMessages\bf2.txt");
            await ReplyAsync(bf2Pastas[random.Next(bf2Pastas.Length)]);
        }
    }
}