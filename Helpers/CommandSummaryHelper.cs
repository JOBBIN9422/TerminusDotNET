using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerminusDotNetCore.Helpers
{
    public static class CommandSummaryHelper
    {
        public static CommandService CommandService { get; set; }

        public static List<Embed> GenerateHelpEmbeds(string commandName = null)
        {
            //if no command name was given, print help for all commands
            if (string.IsNullOrEmpty(commandName))
            {
                IEnumerable<CommandInfo> commands = CommandService.Commands.OrderBy(c => c.Aliases.First());
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
                    AddCommandSummary(embedBuilder, command);
                }

                if (helpTexts.Count == 0 || !helpTexts.Contains(embedBuilder.Build()))
                {
                    helpTexts.Add(embedBuilder.Build());
                }

                return helpTexts;
            }
            else
            {
                //search for the given command by name
                SearchResult cmdSearchResult = CommandService.Search(commandName);
                if (!cmdSearchResult.IsSuccess)
                {
                    return null;
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
                        AddCommandSummary(embedBuilder, commandMatch.Command);
                    }

                    if (helpTexts.Count == 0 || !helpTexts.Contains(embedBuilder.Build()))
                    {
                        helpTexts.Add(embedBuilder.Build());
                    }

                    return helpTexts;
                }
            }
        }

        private static void AddCommandSummary(EmbedBuilder embedBuilder, CommandInfo command)
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
                    commandText += $"\n- `{param.Name}` (`{param.Type.Name}`, optional = `{param.IsOptional}`{defaultVal}): {param.Summary}";
                }
            }

            embedBuilder.AddField($"`{command.Aliases.First()}`", commandText);
        }

        public static void AddCommandSummary(EmbedBuilder embedBuilder, string commandName)
        {
            SearchResult cmdSearchResult = CommandService.Search(commandName);
            if (!cmdSearchResult.IsSuccess)
            {
                return;
            }

            foreach (CommandMatch commandMatch in cmdSearchResult.Commands)
            {
                AddCommandSummary(embedBuilder, commandMatch.Command);
            }
        }
    }
}
