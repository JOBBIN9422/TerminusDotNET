using Discord;
using System;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Helpers
{
    public class Logger
    {
        public static Task Log(LogMessage message)
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
    }
}
