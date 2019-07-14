using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace TerminusDotNetConsoleApp
{
    class Bot
    {
        private DiscordSocketClient _client;

        public Bot()
        {
            //Initialize();
        }

        public async Task Initialize()
        {
            //instantiate client and register log event handler
            _client = new DiscordSocketClient();
            _client.Log += Log;
            _client.MessageReceived += MessageReceived;

            //log in & start the client
            await _client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["DiscordToken"]);
            await _client.StartAsync();

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

        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("fuck you");
            }
        }
    }
}
