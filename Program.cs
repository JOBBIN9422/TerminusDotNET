using System;
using System.Threading.Tasks;

namespace TerminusDotNetCore
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

    }
}
