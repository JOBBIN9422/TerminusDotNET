using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TerminusDotNetCore.Attributes;

namespace TerminusDotNetCore.Helpers
{
    public static class CommandSummaryHelper
    {
        public static CommandService CommandService { get; set; }

        /// <summary>
        /// Generate help embed(s). If no command name is given, generate embeds for all available commands.
        /// </summary>
        /// <param name="commandName">Command to generate help embed(s) for.</param>
        /// <returns>List of formatted embeds containing command help.</returns>
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
        /// <summary>
        /// Add a summary of the command to the given embed builder.
        /// </summary>
        /// <param name="embedBuilder">Embed builder to add command summary fields to.</param>
        /// <param name="command">Command to generate a summary for.</param>
        private static void AddCommandSummary(EmbedBuilder embedBuilder, CommandInfo command)
        {
            string commandText = command.Summary ?? "No description available.\n";

            IReadOnlyList<Discord.Commands.ParameterInfo> parameters = command.Parameters;

            //add each command parameter to the help embed
            foreach (Discord.Commands.ParameterInfo param in parameters)
            {
                if (param.Type.GetCustomAttribute(new NamedArgument().GetType()) != null)
                {
                    //loop over the properties (named parameters) of the named argument obj
                    foreach (var propInfo in param.Type.GetProperties())
                    {
                        //fetch default value if it's set for the current property
                        string defaultVal = "";

                        try
                        {
                            object defaultValueInstance = Activator.CreateInstance(propInfo.PropertyType);
                            defaultVal = defaultValueInstance == null ? string.Empty : $", default = `{defaultValueInstance}`";
                        }
                        catch (MissingMethodException)
                        {
                            defaultVal = string.Empty;
                        }

                        commandText += $"\n- `{propInfo.Name}` (`{propInfo.PropertyType.Name}`, `{defaultVal}): {propInfo.GetCustomAttribute<Description>().Text ?? ""}";

                    }
                }
                else
                {
                    //if there is a default value, print it
                    string defaultVal = param.DefaultValue == null ? string.Empty : $", default = `{param.DefaultValue}`";

                    //add parameter details
                    if (param.Summary != null)
                    {
                        commandText += $"\n- `{param.Name}` (`{param.Type.Name}`, optional = `{param.IsOptional}`{defaultVal}): {param.Summary}";
                    }
                }
            }

            embedBuilder.AddField($"`{command.Aliases.First()}`", commandText);
        }

        /// <summary>
        /// Add a summary of the command to the given embed builder.
        /// </summary>
        /// <param name="embedBuilder">Embed builder to add command summary fields to.</param>
        /// <param name="commandName">Name of the command to search for.</param>
        public static void AddCommandSummary(EmbedBuilder embedBuilder, string commandName)
        {
            SearchResult cmdSearchResult = CommandService.Search(commandName);
            if (!cmdSearchResult.IsSuccess)
            {
                return;
            }

            foreach (CommandMatch commandMatch in cmdSearchResult.Commands)
            {
                if (commandMatch.Alias == commandName)
                {
                    AddCommandSummary(embedBuilder, commandMatch.Command);
                }
            }
        }

        /// <summary>
        /// Add fields which summarize each command under the given group prefix.
        /// </summary>
        /// <param name="type">The module tagged with the group prefix.</param>
        /// <param name="embedBuilder">Embed builder to add command summary fields to.</param>
        /// <param name="groupPrefix">The module group prefix.</param>
        public static void GenerateGroupCommandSummary(Type type, EmbedBuilder embedBuilder, string groupPrefix)
        {
            MethodInfo[] methods = type.GetMethods();

            //choose only methods with the 'Command' attribute
            foreach (MethodInfo method in methods)
            {
                Attribute attribute = method.GetCustomAttributes().Where(a => a is CommandAttribute).FirstOrDefault();
                CommandAttribute cmdAttribute = attribute as CommandAttribute;

                //add a summary of the command searched by its full name
                if (cmdAttribute != null && !string.IsNullOrEmpty(cmdAttribute.Text))
                {
                    AddCommandSummary(embedBuilder, $"{groupPrefix} {cmdAttribute.Text}");
                }
            }
        }
    }
}
