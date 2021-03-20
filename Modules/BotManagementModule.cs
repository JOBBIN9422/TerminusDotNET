using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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

        [Command("regex", RunMode = RunMode.Async)]
        [Summary("Set the regex response behavior of the bot.")]
        public async Task SetRegexState([Summary("Set the regex response behavior. Possible values: `on/off`, `yes/no`, `y/n`, `enabled/disabled`")]string state = null)
        {
            if (string.IsNullOrEmpty(state))
            {
                //treat the command as a query if no setting is provided
                string regexState = _bot.IsRegexActive ? "enabled" : "disabled";
                await ReplyAsync($"Regex responses are currently {regexState}.");
                return;
            }

            JObject configObj = ConfigHelper.ReadConfig();
            state = state.ToLower();
            if (state == "off" || state == "n" || state == "no" || state == "disabled")
            {
                //set the regex state and write it to config
                _bot.IsRegexActive = false;
                configObj["RegexEnabled"] = _bot.IsRegexActive;
                ConfigHelper.UpdateConfig(configObj);

                await ReplyAsync("Disabled regex responses.");
            }
            else if (state == "on" || state == "y" || state == "yes" || state == "enabled")
            {
                _bot.IsRegexActive = true;
                configObj["RegexEnabled"] = _bot.IsRegexActive;
                ConfigHelper.UpdateConfig(configObj);
                await ReplyAsync("Enabled regex responses.");
            }
            else
            {
                return;
            }
        }

        [Command("about")]
        [Summary("Display information about the bot.")]
        public async Task DisplayBotInfo()
        {
            RestApplication appInfo = await _bot.Client.GetApplicationInfoAsync();
            EmbedBuilder builder = new EmbedBuilder()
            {
                Title = "About Me"
            };

            TimeSpan uptime = DateTime.Now - _bot.StartTime;

            builder.AddField("Name: ", $"`{appInfo.Name}`");
            builder.AddField("Created at: ", $"`{appInfo.CreatedAt}`");
            builder.AddField("Owner: ", $"`{appInfo.Owner}`");
            builder.AddField("Discord.Net Version: ", $"`{DiscordConfig.Version}`");
            builder.AddField("Discord API Version: ", $"`{DiscordConfig.APIVersion}`");
            builder.AddField("Uptime: ", $"`{uptime.ToString("%d")} days, {uptime.ToString("%h")} hours, {uptime.ToString("%m")} minutes`");

            await ReplyAsync(embed: builder.Build());
        }

        [Command("libs", RunMode = RunMode.Async)]
        public async Task ListLibraries()
        {
            //need a list of embeds since each embed can only have 25 fields max
            List<Embed> libList = new List<Embed>();
            int entryCount = 0;

            var embed = new EmbedBuilder
            {
                Title = $"{_bot.InstalledLibraries.Count} Installed Libraries"
            };


            foreach (var library in _bot.InstalledLibraries)
            {
                entryCount++;

                //if we have 25 entries in an embed already, need to make a new one 
                if (entryCount % EmbedBuilder.MaxFieldCount == 0 && entryCount > 0)
                {
                    libList.Add(embed.Build());
                    embed = new EmbedBuilder();
                }

                embed.AddField(library.Key, $"`{library.Value}`");
            }

            //add the most recently built embed if it's not in the list yet 
            if (libList.Count == 0 || !libList.Contains(embed.Build()))
            {
                libList.Add(embed.Build());
            }

            foreach (var libEmbed in libList)
            {
                await ReplyAsync(embed: libEmbed);
            }
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

            [Command]
            [Summary("Display usage info about the `log` command.")]
            public async Task PrintUsageInfo()
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                CommandSummaryHelper.GenerateGroupCommandSummary(GetType(), embedBuilder, "log");
                await ReplyAsync(embed: embedBuilder.Build());
            }

            [Command("console", RunMode = RunMode.Async)]
            [Summary("Download the most recent stdout log file, if any exist.")]
            public async Task DownloadMostRecentConsoleLog()
            {
                await DownloadMostRecentLog(Logger.ConsoleLogDir);
            }

            [Command("errors", RunMode = RunMode.Async)]
            [Summary("Download the most recent command error log file, if any exist.")]
            public async Task DownloadMostRecentErrorLog()
            {
                await DownloadMostRecentLog(Logger.ErrorLogDir);
            }

            [Command("stats", RunMode = RunMode.Async)]
            [Summary("Get information about the total count and size (KB) of log files currently stored on the server.")]
            public async Task GetLogStats([Summary("The type of logs to filter on. Possible values: `console, errors, all (default value)`")]string logType = "all")
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

                await ReplyAsync($"{fileCount} log files totaling {totalSize / 1024.0:0.##} KB.");
            }

            [Command("clean", RunMode = RunMode.Async)]
            [Summary("Delete log files for the given log type.")]
            public async Task CleanLogFiles([Summary("The type of logs to delete. Possible values: `console, errors, all (default value)`")]string logType = "all")
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
                    file.Delete();
                }

                await ReplyAsync($"Deleted {fileCount} log files totaling {totalSize / 1024.0:0.##} KB.");
            }
        }

        [Group("temp")]
        public class TempDirModule : ModuleBase<SocketCommandContext>
        {
            [Command]
            [Summary("Display usage info about the `temp` command.")]
            public async Task PrintUsageInfo()
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                CommandSummaryHelper.GenerateGroupCommandSummary(GetType(), embedBuilder, "temp");
                await ReplyAsync(embed: embedBuilder.Build());
            }

            [Command("list", RunMode = RunMode.Async)]
            [Summary("Print a list of files in the temp assets directory.")]
            public async Task ListTempDir()
            {
                List<string> tempFiles = AttachmentHelper.GetTempAssets();
                if (tempFiles.Count == 0)
                {
                    await ReplyAsync("`empty`");
                }
                else 
                {
                    string fileList = string.Empty;

                    //build a list of filenames (no paths included)
                    foreach (string filePath in tempFiles)
                    {
                        fileList += Path.GetFileName(filePath) + Environment.NewLine;
                    }
                    fileList = $"```{Environment.NewLine}{fileList}{Environment.NewLine}```";

                    await ReplyAsync(fileList);
                } 
            }
            [Command("clean")]
            [Summary("Delete temp files of the given type.")]
            public async Task CleanTempDir([Summary("The type of temp files to delete. Possible values: `images, media, text, all (default value)`")]string filter = "all")
            {
                AttachmentFilter filterType;
                switch (filter)
                {
                    case "all":
                    default:
                        filterType = AttachmentFilter.All;
                        break;
                    case "images":
                        filterType = AttachmentFilter.Images;
                        break;

                    case "media":
                        filterType = AttachmentFilter.Media;
                        break;

                    case "text":
                        filterType = AttachmentFilter.Plaintext;
                        break;
                }
                List<string> files = AttachmentHelper.GetTempAssets(filterType);
                if (files.Count == 0)
                {
                    await ReplyAsync("No temp files to delete.");
                }
                else
                {
                    AttachmentHelper.DeleteFiles(files);
                    await ReplyAsync($"Deleted {files.Count} temp files.");
                }
            }

            [Command("remove")]
            [Summary("Delete a temp file by name, if it exists.")]
            public async Task DeleteTempFile(string filename = "")
            {
                if (string.IsNullOrEmpty(filename))
                {
                    await ReplyAsync("No filename provided.");
                }
                else
                {
                    if (AttachmentHelper.DeleteFile(filename))
                    {
                        await ReplyAsync($"Deleted file `{filename}`.");
                    }
                    else
                    {
                        await ReplyAsync($"File `{filename}` not found.");
                    }
                }
            }
        }

        [Group("client")]
        public class BotClientModule : ModuleBase<SocketCommandContext>
        {
            private Bot _bot;

            public IConfiguration Config { get; set; }

            public BotClientModule(IConfiguration config, Bot bot)
            {
                _bot = bot;
                Config = config;
            }

            [Command("reset", RunMode = RunMode.Async)]
            [Summary("Reset the bot's avatar and nickname.")]
            public async Task ResetBotAvatarAndUsername()
            {
                await _bot.Client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(Path.Combine("assets", "images", "terminus.jpg")));
                await _bot.Client.CurrentUser.ModifyAsync(x => x.Username = "Terminus.NET");
            }

            [Command("mimic", RunMode = RunMode.Async)]
            [Summary("Change the bot's avatar and username to the given user.")]
            public async Task MimicUser(SocketGuildUser user)
            {
                var fileIdString = Guid.NewGuid().ToString("N");
                var avatarPath = Path.Combine("assets", "temp", Guid.NewGuid().ToString("N"));
                using (var webClient = new WebClient())
                {
                    webClient.DownloadFileAsync(new Uri(user.GetAvatarUrl()), avatarPath);
                }

                await _bot.Client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(avatarPath));
                await _bot.Client.CurrentUser.ModifyAsync(x => x.Username = string.IsNullOrEmpty(user.Nickname) ? user.Nickname : user.Username);
            }
        }
    }
}
