using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace TerminusDotNetConsoleApp
{
    class Bot
    {
        private DiscordSocketClient _client;
        private CommandService _commandService;

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
            //_client.MessageReceived += MessageReceived;
            _client.MessageReceived += HandleCommandAsync;

            //log in & start the client
            await _client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["DiscordToken"]);
            await _client.StartAsync();

            //init commands service
            await _commandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);

            //hang out for now
            await Task.Delay(-1);
        }

        //log message to file
        private Task Log(LogMessage message)
        {
            Logger.WriteMessage(message.Source + ".txt", message.ToString());

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

            //check if message is command and not sent by a bot
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                || message.Author.IsBot)
            {
                return;
            }

            //execute the command 
            var context = new SocketCommandContext(_client, message);
            await _commandService.ExecuteAsync(context: context, argPos: argPos, services: null);
        }

        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("fuck you");
            }
        }
    }
}
