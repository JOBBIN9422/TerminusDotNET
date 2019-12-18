using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace TerminusDotNetCore.Modules
{
    public class TextModule
    {
        private Random _random;
        
        public TextModule(Random random)
        {
            _random = random;
        }
        
        public abstract async Task SayAsync();
    }
}
