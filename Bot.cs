using System;
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
        private DiscordSocketClient _client;
        private CommandService _commandService;
        private IServiceProvider _serviceProvider;

        public Bot()
        {
            //Initialize();
        }

        public async Task Initialize()
        {
            _commandService = new CommandService();

            //instantiate client and register log event handler
            _client = new DiscordSocketClient();
            _client.Log += Log;
            _client.MessageReceived += HandleCommandAsync;

            //init config
            IConfiguration config = new ConfigurationBuilder()
                                        .AddJsonFile("appsettings.json", true, true)
                                        .Build();

            //log in & start the client
            string token = config["DiscordToken"];
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            //init custom services
            _serviceProvider = InstallServices();

            //init commands service
            await _commandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _serviceProvider);

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
            if (message == null)
            {
                return;
            }

            //track position of command prefix char 
            int argPos = 0;

            //check if message is not command or not sent by bot
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                || message.Author.IsBot)
            {
                //look for wildcards in the current message 
                var regexMsgParser = new RegexCommands();
                var matches = regexMsgParser.ParseMessage(message.Content);

                if (matches.Count > 0 && !message.Author.IsBot)
                {
                    foreach (var match in matches)
                    {
                        await message.Channel.SendMessageAsync(match);
                    }
                }
                return;
            }

            //handle commands
            var context = new SocketCommandContext(_client, message);
            var commandResult = await _commandService.ExecuteAsync(context: context, argPos: argPos, services: _serviceProvider);
        }

        private IServiceProvider InstallServices()
        {
            var serviceCollection = new ServiceCollection();

            //new custom services get added here
            serviceCollection.AddSingleton<ImageService>()
                             .AddSingleton<WideTextService>();
            //serviceCollection.AddSingleton<WideTextService>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
