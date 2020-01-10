using System;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using TerminusDotNetCore.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.FileExtensions;

namespace TerminusDotNetCore
{
    class Bot
    {
        private RegexCommands _regexMsgParser;
        private DiscordSocketClient _client;
        private CommandService _commandService;
        private IServiceProvider _serviceProvider;
        private bool _isActive = true;
        private List<ulong> _blacklistChannels = new List<ulong>();

        //public Bot()
        //{
        //}

        public async Task Initialize()
        {
            _regexMsgParser = new RegexCommands();
            _commandService = new CommandService();

            //instantiate client and register log event handler
            _client = new DiscordSocketClient();
            _client.Log += Log;
            _client.MessageReceived += HandleCommandAsync;

            //init config
            IConfiguration config = new ConfigurationBuilder()
                                        .AddJsonFile("appsettings.json", true, true)
                                        .Build();

            //init client secrets
            IConfiguration secrets = new ConfigurationBuilder()
                                        .AddJsonFile("secrets.json", true, true)
                                        .Build();

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
                if (config[configEntry.Key] == null)
                {
                    await Log(new LogMessage(LogSeverity.Warning, "appsettings.json", $"WARN: Missing item in appsettings config file :: {configEntry.Key} --- Description :: {configEntry.Value}"));
                }
            }

            //alert in console for each missing client secret field
            foreach (var secretEntry in requiredSecrets)
            {
                if (secrets[secretEntry.Key] == null)
                {
                    await Log(new LogMessage(LogSeverity.Warning, "secrets.json", $"WARN: Missing item in secrets file :: {secretEntry.Key} --- Description :: {secretEntry.Value}"));
                }
            }

            //log in & start the client
            string token = secrets["DiscordToken"];
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            //load blacklisted channels
            var blacklistSection = config.GetSection("BlacklistChannels");
            foreach (var section in blacklistSection.GetChildren())
            {
                ulong id = ulong.Parse(section.Value);
                _blacklistChannels.Add(id);
            }

            //init custom services
            _serviceProvider = InstallServices();

            //init commands service
            await _commandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _serviceProvider);
            _commandService.CommandExecuted += OnCommandExecutedAsync;

            //hang out for now
            await Task.Delay(-1);
        }

        //log message to file
        private Task Log(LogMessage message)
        {
            //Logger.WriteMessage(message.Source + ".txt", message.ToString());

            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Debug:
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                default:
                    break;
            }
            Console.WriteLine(message.ToString());
            Console.ResetColor();
            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            
            //don't act in blacklisted channels
            if (message == null || _blacklistChannels.Contains(message.Channel.Id))
            {
                return;
            }

            //check for bot state pseudo-commands 
            if (message.Content == "!die")
            {
                await DisableBot(message);
            }
            else if (message.Content == "!live")
            {
                await EnableBot(message);
            }
            else if (_isActive)
            {
                //track position of command prefix char 
                int argPos = 0;

                //look for regex matches and reply if any are found
                await HandleRegexResponses(message);

                //check if message is not command or not sent by bot
                if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                || message.Author.IsBot)
                {
                    return;
                }

                //handle commands
                var context = new SocketCommandContext(_client, message);
                var commandResult = await _commandService.ExecuteAsync(context: context, argPos: argPos, services: _serviceProvider);
            }
        }

        private async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            try
            {
                if (!result.IsSuccess && result is ExecuteResult execResult)
                {
                    //alert user and print error details to console
                    await context.Channel.SendMessageAsync(result.ErrorReason);
                    await Log(new LogMessage(LogSeverity.Error, "CommandExecution", $"Error in command '{command.Value.Name}': {execResult.ErrorReason}"));
                    await Log(new LogMessage(LogSeverity.Error, "CommandExecution", $"Exception details (see errors.txt): {execResult.Exception.StackTrace}"));
                
                    //dump exception details to error log
                    using (StreamWriter writer = new StreamWriter("errors.txt", true))
                    {
                        writer.WriteLine("----- BEGIN ENTRY -----");
                        writer.WriteLine($"ERROR DATETIME: {DateTime.Now.ToString()}");
                        writer.WriteLine($"COMMAND NAME  : {command.Value.Name}");
                        writer.WriteLine();
                        writer.WriteLine(execResult.Exception.ToString());
                        writer.WriteLine("----- END ENTRY   -----");
                        writer.WriteLine();
                    }
                }
                else
                {
                    //on successful command execution
                    await Log(new LogMessage(LogSeverity.Info, "CommandExecution", $"Command '{command.Value.Name}' executed successfully."));
                }
            }
            catch (InvalidOperationException)
            {
                await context.Channel.SendMessageAsync("Unknown command.");
                await Log(new LogMessage(LogSeverity.Error, "CommandExecution", $"Unknown command."));
            }
        }

        private IServiceProvider InstallServices()
        {
            var serviceCollection = new ServiceCollection();

            //new custom services get added here
            serviceCollection.AddSingleton<ImageService>()
                             .AddSingleton<WideTextService>()
                             .AddSingleton<TwitterService>()
                             .AddSingleton<AudioService>()
                             .AddSingleton<MarkovService>()
                             .AddSingleton<TicTacToeService>()
                             .AddSingleton(new Random());
            //serviceCollection.AddSingleton<WideTextService>();

            return serviceCollection.BuildServiceProvider();
        }

        private async Task DisableBot(SocketUserMessage message)
        {
            //disable the bot and set the status to idle
            _isActive = false;
            await message.Channel.SendMessageAsync("aight, I'm finna head out...");
            await _client.SetStatusAsync(UserStatus.Idle);
            await Log(new LogMessage(LogSeverity.Info, "HandleCommand", $"Going to sleep..."));
        }
        
        private async Task EnableBot(SocketUserMessage message)
        {
            //only respond if we're actually asleep
            if (!_isActive)
            {
                await message.Channel.SendMessageAsync("real shit?");
            }
               
            //re-enable the bot and set status accordingly
            _isActive = true;
            await _client.SetStatusAsync(UserStatus.Online);
            await Log(new LogMessage(LogSeverity.Info, "HandleCommand", $"Resuming..."));
        }
        
        //check the given message for regex matches and send responses accordingly
        private async Task HandleRegexResponses(SocketUserMessage message)
        {
            //don't respond to bots (maybe change this to only ignore itself)
            if (message.HasCharPrefix('!', ref int x))
            {
                return;
            }
            
            //look for wildcards in the current message 
            var matches = _regexMsgParser.ParseMessage(message.Content);

            //respond for each matching regex
            if (matches.Count > 0 && !message.Author.IsBot)
            {
                foreach (var match in matches)
                {
                    await message.Channel.SendMessageAsync(match);
                }
            }
        }
    }
}
