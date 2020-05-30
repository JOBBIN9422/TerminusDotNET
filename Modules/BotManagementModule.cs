using Discord;
using Discord.Commands;
using Discord.Rest;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Helpers;

namespace TerminusDotNetCore.Modules
{
    public class BotManagementModule : ModuleBase<SocketCommandContext>
    {
        private Bot _bot;

        public IConfiguration Config { get; set; }

        public BotManagementModule(IConfiguration config, Bot bot)
        {
            _bot = bot;
            Config = config;
        }

        [Command("regex")]
        public async Task SetRegexState(string state = null)
        {
            if (string.IsNullOrEmpty(state))
            {
                //treat the command as a query if no setting is provided
                string regexState = _bot.IsRegexActive ? "enabled" : "disabled";
                await ReplyAsync($"Regex responses are currently {regexState}.");
                return;
            }
            state = state.ToLower();

            if (state == "off" || state == "n" || state == "no" || state == "disabled")
            {
                _bot.IsRegexActive = false;
                await ReplyAsync("Disabled regex responses.");
            }
            else if (state == "on" || state == "y" || state == "yes" || state == "enabled")
            {
                _bot.IsRegexActive = true;
                await ReplyAsync("Enabled regex responses.");
            }
            else
            {
                return;
            }
        }

        [Command("about")]
        public async Task DisplayBotInfo()
        {
            RestApplication appInfo = await _bot.Client.GetApplicationInfoAsync();
            EmbedBuilder builder = new EmbedBuilder()
            {
                Title = "About Me"
            };

            TimeSpan uptime = DateTime.Now - _bot.StartTime;

            builder.AddField("Name: ", appInfo.Name);
            builder.AddField("Created at: ", appInfo.CreatedAt);
            builder.AddField("Owner: ", appInfo.Owner);
            builder.AddField("Discord.Net Version: ", DiscordConfig.Version);
            builder.AddField("Discord API Version: ", DiscordConfig.APIVersion);
            builder.AddField("Uptime: ", $"{uptime.ToString("%d")} days, {uptime.ToString("%h")} hours, {uptime.ToString("%m")} minutes");

            await ReplyAsync(embed: builder.Build());
        }

        [Group("log")]
        public class LogModule : ModuleBase<SocketCommandContext>
        {
            private async Task DownloadMostRecentLog(string dirName)
            {
                try
                {
                    //get most recent log file
                    string mostRecentFilename = new DirectoryInfo(dirName).GetFiles().OrderByDescending(f => f.CreationTime).First().FullName;

                    //send log file
                    await Context.Channel.SendFileAsync(mostRecentFilename);
                }
                catch (InvalidOperationException)
                {
                    await ReplyAsync("No log files currently exist.");
                }
            }

            [Command("console")]
            public async Task DownloadMostRecentConsoleLog()
            {
                await DownloadMostRecentLog(Logger.ConsoleLogDir);
            }

            [Command("errors")]
            public async Task DownloadMostRecentErrorLog()
            {
                await DownloadMostRecentLog(Logger.ErrorLogDir);
            }

            [Command("stats")]
            public async Task GetLogStats(string logType = "all")
            {
                DirectoryInfo logDirInfo = null;
                switch (logType)
                {
                    case "console":
                        logDirInfo = new DirectoryInfo(Logger.ConsoleLogDir);
                        break;

                    case "errors":
                        logDirInfo = new DirectoryInfo(Logger.ErrorLogDir);
                        break;

                    case "all":
                    default:
                        logDirInfo = new DirectoryInfo(Logger.RootLogDir);
                        break;
                }

                int fileCount = 0;
                long totalSize = 0;
                foreach (var file in logDirInfo.GetFiles("*.txt", SearchOption.AllDirectories))
                {
                    fileCount++;
                    totalSize += file.Length;
                }

                await ReplyAsync($"{fileCount} logs totaling {totalSize / 1024.0:0.##} KB.");
            }
        }
    }
}
