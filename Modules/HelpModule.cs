using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Helpers;

namespace TerminusDotNetCore.Modules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        [Command("help", RunMode = RunMode.Async)]
        [Summary("List information about a given command (if it exists).")]
        public async Task Help([Summary("the name of the command to search for")][Remainder]string commandName = null)
        {
            List<Embed> helpTexts = CommandSummaryHelper.GenerateHelpEmbeds(commandName);
            if (helpTexts == null)
            {
                await ReplyAsync("No command with the given name was found. (Usage: !help <command-name>)");
            }
            else
            {
                foreach (var helpEmbed in helpTexts)
                {
                    await ReplyAsync(embed: helpEmbed);
                }
            }
        }
    }
}
