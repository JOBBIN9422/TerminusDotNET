using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using TerminusDotNetCore.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TerminusDotNetCore.Helpers;
using System.Xml.Linq;
using System.Linq;

namespace TerminusDotNetCore
{
    public class Bot
    {
        public DateTime StartTime { get; private set; }

        public DiscordSocketClient Client { get; private set; }

        //Discord.NET command management
        public CommandService CommandService { get; private set; }

        public bool IsRegexActive { get; set; } = true;

        public Dictionary<string, string> InstalledLibraries { get; private set; } = new Dictionary<string, string>();

        //command services
        private IServiceProvider _serviceProvider;

        //for detecting regex matches in messages
        private RegexCommands _regexMsgParser;

        //ignored channels
        private List<ulong> _blacklistChannels = new List<ulong>();

        private IConfiguration _config = new ConfigurationBuilder()
                                        .AddJsonFile("appsettings.json", true, true)
                                        .AddJsonFile("secrets.json", true, true)
                                        .Build();

        public async Task Initialize()
        {
            StartTime = DateTime.Now;

            //load libraries into version dict
            PopulateInstalledLibrariesList();

            _regexMsgParser = new RegexCommands();
            CommandService = new CommandService();

            //set command summary helper's command service ref
            CommandSummaryHelper.CommandService = CommandService;

            //instantiate client and register log event handler
            Client = new DiscordSocketClient();
            Client.Log += Logger.Log;
            Client.MessageReceived += HandleCommandAsync;

            //verify that each required client secret is in the secrets file
            Dictionary<string, string> requiredSecrets = new Dictionary<string, string>()
            {
                {"DiscordToken", "Token to connect to your discord server"}
            };

            //verify that each required config entry is in the appsettings file
            Dictionary<string, string> requiredConfigs = new Dictionary<string, string>()
            {
                {"FfmpegCommand", "should be ffmpeg.exe for windows, ffmpeg for linux"},
                {"AudioChannelId", "ID of main audio channel to play audio in"},
                {"WeedChannelId", "ID of weed sesh audio channel"}
            };

            //alert in console for each missing config field
            foreach (var configEntry in requiredConfigs)
            {
                if (_config[configEntry.Key] == null)
                {
                    await Logger.Log(new LogMessage(LogSeverity.Warning, "appsettings.json", $"WARN: Missing item in appsettings config file :: {configEntry.Key} --- Description :: {configEntry.Value}"));
                }
            }

            //alert in console for each missing client secret field
            foreach (var secretEntry in requiredSecrets)
            {
                if (_config[secretEntry.Key] == null)
                {
                    await Logger.Log(new LogMessage(LogSeverity.Warning, "secrets.json", $"WARN: Missing item in secrets file :: {secretEntry.Key} --- Description :: {secretEntry.Value}"));
                }
            }

            //load regex state from config
            IsRegexActive = bool.Parse(_config["RegexEnabled"]);

            //log in & start the client
            string token = _config["DiscordToken"];
            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

            //load blacklisted channels
            var blacklistSection = _config.GetSection("BlacklistChannels");
            foreach (var section in blacklistSection.GetChildren())
            {
                ulong id = ulong.Parse(section.Value);
                _blacklistChannels.Add(id);
            }

            //init custom services
            _serviceProvider = InstallServices();

            //init commands service
            await CommandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _serviceProvider);
            CommandService.CommandExecuted += OnCommandExecutedAsync;

            //hang out for now
            await Task.Delay(-1);
        }

        //log message to file

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;

            //don't act in blacklisted channels
            if (message == null || _blacklistChannels.Contains(message.Channel.Id))
            {
                return;
            }

            //track position of command prefix char 
            int argPos = 0;

            //look for regex matches and reply if any are found
            if (IsRegexActive)
            {
                await HandleRegexResponses(message);
            }

            //check if message is not command or not sent by bot
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(Client.CurrentUser, ref argPos))
            || message.Author.IsBot)
            {
                return;
            }

            //handle commands
            var context = new SocketCommandContext(Client, message);
            var commandResult = await CommandService.ExecuteAsync(context: context, argPos: argPos, services: _serviceProvider);
        }

        private async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            try
            {
                string cmdDisplayName = $"{command.Value.Module.Group ?? ""} {command.Value.Name}";
                if (!result.IsSuccess && result is ExecuteResult execResult)
                {
                    //alert user and print error details to console
                    await context.Channel.SendMessageAsync(result.ErrorReason);
                    await Logger.Log(new LogMessage(LogSeverity.Error, "CommandExecution", $"Error in command '{cmdDisplayName}': {execResult.ErrorReason}"));
                    await Logger.Log(new LogMessage(LogSeverity.Error, "CommandExecution", $"Exception details (see errors.txt):\n {execResult.Exception.StackTrace}"));

                    //dump exception details to error log
                    string currLogFilename = $"errors_{DateTime.Today.ToString("MM-dd-yyyy")}.txt";
                    using (StreamWriter writer = new StreamWriter(Path.Combine(Logger.ErrorLogDir,currLogFilename), true))
                    {
                        writer.WriteLine("----- BEGIN ENTRY -----");
                        writer.WriteLine($"ERROR DATETIME: {DateTime.Now.ToString()}");
                        writer.WriteLine($"COMMAND NAME  : {cmdDisplayName}");
                        writer.WriteLine();
                        writer.WriteLine(execResult.Exception.ToString());
                        writer.WriteLine("----- END ENTRY   -----");
                        writer.WriteLine();
                    }
                }
                else
                {
                    //on successful command execution
                    await Logger.Log(new LogMessage(LogSeverity.Info, "CommandExecution", $"Command '{cmdDisplayName}' executed successfully."));
                }
            }
            catch (InvalidOperationException)
            {
                await context.Channel.SendMessageAsync($"Unknown command.");
                await Logger.Log(new LogMessage(LogSeverity.Error, "CommandExecution", $"Unknown command."));
            }
        }

        private IServiceProvider InstallServices()
        {
            var serviceCollection = new ServiceCollection();

            //new custom services (and objects passed via DI) get added here
            serviceCollection.AddSingleton(_config)
                             .AddSingleton(Client)
                             .AddSingleton<ImageService>()
                             .AddSingleton<TextEditService>()
                             .AddSingleton<TwitterService>()
                             .AddSingleton<AudioService>()
                             .AddSingleton<MarkovService>()
                             .AddSingleton<TicTacToeService>()
                             .AddSingleton<IronPythonService>()
                             .AddSingleton(new Random())
                             .AddSingleton(this);

            return serviceCollection.BuildServiceProvider();
        }

        //check the given message for regex matches and send responses accordingly
        private async Task HandleRegexResponses(SocketUserMessage message)
        {
            //don't respond to bots (maybe change this to only ignore itself)
            int charPos = 0;
            if (message.HasCharPrefix('!', ref charPos))
            {
                return;
            }

            //look for wildcards in the current message 
            List<Tuple<string, string>> matches = _regexMsgParser.ParseMessage(message.Content);

            //respond for each matching regex
            if (matches.Count > 0 && !message.Author.IsBot)
            {
                foreach (var match in matches)
                {
                    await message.Channel.SendMessageAsync(match.Item1);
                    if (!string.IsNullOrEmpty(match.Item2))
                    {
                        //play the audio file specified
                        AudioService audioService = _serviceProvider.GetService(typeof(AudioService)) as AudioService;
                        audioService.Guild = (message.Channel as SocketGuildChannel).Guild;

                        _ = audioService.PlayRegexAudio(match.Item2);
                    }
                }
            }
        }

        private void PopulateInstalledLibrariesList()
        {
            //open the .csproj
            XNamespace msBuild = "http://schemas.microsoft.com/developer/msbuild/2003";
            XDocument projectDoc = XDocument.Load("TerminusDotNetCore.csproj");

            //get nuget package names
            IEnumerable<string> references = projectDoc.Element("Project")
                                                       .Element("ItemGroup")
                                                       .Elements("PackageReference")
                                                       .Attributes("Include")
                                                       .Select(e => e.Value);
            //get nuget package versions
            IEnumerable<string> versions = projectDoc.Element("Project")
                                                       .Element("ItemGroup")
                                                       .Elements("PackageReference")
                                                       .Attributes("Version")
                                                       .Select(e => e.Value);

            //zip into dict of package-version pairs
            InstalledLibraries = references.Zip(versions, (pkg, version) => new { pkg, version })
                .ToDictionary(x => x.pkg, x => x.version);
        }
    }
}
