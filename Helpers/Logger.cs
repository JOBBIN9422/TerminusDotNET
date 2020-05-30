using Discord;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Helpers
{
    public class Logger
    {
        public static readonly string RootLogDir = "logs";
        public static readonly string ConsoleLogDir = Path.Combine("logs", "console");
        public static readonly string ErrorLogDir = Path.Combine("logs", "errors");

        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

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
            LogToFile(message);
            return Task.CompletedTask;
        }

        private static void LogToFile(LogMessage message)
        {
            _readWriteLock.EnterWriteLock();

            try
            {
                string currLogFilename = $"log_{DateTime.Today.ToString("MM-dd-yyyy")}.txt";
                using (StreamWriter writer = new StreamWriter(Path.Combine(ConsoleLogDir, currLogFilename), true))
                {
                    writer.WriteLine(message.ToString());
                }
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
        }
    }
}
