using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetConsoleApp
{
    class Program
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        //do regular main shit here
        public async Task MainAsync()
        {
            Bot bot = new Bot();
            await bot.Initialize();
        }

        //FIX: move me somewhere else?
        
    }
}
