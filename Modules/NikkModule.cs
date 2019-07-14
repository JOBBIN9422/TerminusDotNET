using Discord.Commands;
using System.Threading.Tasks;

namespace TerminusDotNetConsoleApp.Modules
{
    public class NikkModule : ModuleBase<SocketCommandContext>
    {
        [Command("nikk")]
        [Summary("Echoes a spicy nikk quote.")]
        public async Task SayAsync()
        {
            await Context.Channel.SendMessageAsync("it's me, nikk");
        }
    }
}
