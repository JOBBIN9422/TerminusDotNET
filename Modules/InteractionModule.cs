using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Modules
{
    public class InteractionModule : InteractionModuleBase
    {
        public IConfiguration Config { get; set; }

        public InteractionModule(IConfiguration config)
        {
            Config = config;
        }
    }
}
