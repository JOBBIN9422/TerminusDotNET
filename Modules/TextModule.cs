using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace TerminusDotNetCore.Modules
{
    public abstract class TextModule : ModuleBase<SocketCommandContext>
    {
        protected Random _random;
        
        public TextModule(Random random)
        {
            _random = random;
        }
        
        public abstract Task SayAsync();
    }
}
