using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Modules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private CommandService _commandService;

        public HelpModule(CommandService service)
        {
            _commandService = service;
        }

        [Command("help", RunMode = RunMode.Async)]
        [Summary("List information about a given command (if it exists).")]
        public async Task Help([Summary("the name of the command to search for")]string commandName = null)
        {
            //if no command name was given, print help for all commands
            if (string.IsNullOrEmpty(commandName))
            {
                IEnumerable<CommandInfo> commands = _commandService.Commands;
                EmbedBuilder embedBuilder = new EmbedBuilder();

                foreach (CommandInfo command in commands)
                {
                    AddCommandField(embedBuilder, command);
                }

                await ReplyAsync("Available commands: ", false, embedBuilder.Build());
            }
            else
            {
                //search for the given command by name
                SearchResult cmdSearchResult = _commandService.Search(commandName);
                if (!cmdSearchResult.IsSuccess)
                {
                    await ReplyAsync("No command with the given name was found. (Usage: !help <command-name>)");
                }
                else
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder();

                    foreach (CommandMatch commandMatch in cmdSearchResult.Commands)
                    {
                        AddCommandField(embedBuilder, commandMatch.Command);
                    }

                    await ReplyAsync("Available commands: ", false, embedBuilder.Build());
                }
            }
        }

        private void AddCommandField(EmbedBuilder embedBuilder, CommandInfo command)
        {
            string commandText = command.Summary ?? "No description available.\n";

            IReadOnlyList<ParameterInfo> parameters = command.Parameters;

            //add each command parameter to the help embed
            foreach (ParameterInfo param in parameters)
            {
                if (param.Summary != null)
                {
                    commandText += $"\n- {param.Name} ({param.Type.Name}, optional = {param.IsOptional}): {param.Summary}";
                }
            }

            embedBuilder.AddField(command.Name, commandText);
        }
    }
}
