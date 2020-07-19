using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task Help([Summary("the name of the command to search for")][Remainder]string commandName = null)
        {
            //if no command name was given, print help for all commands
            if (string.IsNullOrEmpty(commandName))
            {
                IEnumerable<CommandInfo> commands = _commandService.Commands;
                List<Embed> helpTexts = new List<Embed>();
                EmbedBuilder embedBuilder = new EmbedBuilder();

                foreach (CommandInfo command in commands)
                {
                    //create a new embed if we've hit the field limit for the current embed
                    if (embedBuilder.Fields.Count % EmbedBuilder.MaxFieldCount == 0 && embedBuilder.Fields.Count > 0)
                    {
                        helpTexts.Add(embedBuilder.Build());
                        embedBuilder = new EmbedBuilder();
                    }

                    //add a summary field of the current cmd to the current embed
                    AddCommandField(embedBuilder, command);
                }

                if (helpTexts.Count == 0 || !helpTexts.Contains(embedBuilder.Build()))
                {
                    helpTexts.Add(embedBuilder.Build());
                }

                foreach (var helpEmbed in helpTexts)
                {
                    await ReplyAsync(embed: helpEmbed);
                }
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
                    List<Embed> helpTexts = new List<Embed>();
                    EmbedBuilder embedBuilder = new EmbedBuilder();

                    foreach (CommandMatch commandMatch in cmdSearchResult.Commands)
                    {
                        //create a new embed if we've hit the field limit for the current embed
                        if (embedBuilder.Fields.Count % EmbedBuilder.MaxFieldCount == 0 && embedBuilder.Fields.Count > 0)
                        {
                            helpTexts.Add(embedBuilder.Build());
                            embedBuilder = new EmbedBuilder();
                        }

                        //add a summary field of the current cmd to the current embed
                        AddCommandField(embedBuilder, commandMatch.Command);
                    }

                    if (helpTexts.Count == 0 || !helpTexts.Contains(embedBuilder.Build()))
                    {
                        helpTexts.Add(embedBuilder.Build());
                    }

                    //send the list of embeds
                    foreach (var helpEmbed in helpTexts)
                    {
                        await ReplyAsync(embed: helpEmbed);
                    }
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
                //if there is a default value, print it
                string defaultVal = param.DefaultValue == null ? string.Empty : $", default = `{param.DefaultValue}`";
                if (param.Summary != null)
                {
                    commandText += $"\n- `{param.Name}` ({param.Type.Name}, optional = `{param.IsOptional}`{defaultVal}): {param.Summary}";
                }
            }

            embedBuilder.AddField($"`{command.Aliases.First()}`", commandText);
        }
    }
}
