using Discord;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Modules;

namespace TerminusDotNetCore.Services
{
    public interface ICustomService
    {
        public IConfiguration Config { get; set; }
        ServiceControlModule ParentModule { get; set; }

        //Task ServiceReplyAsync(string s, EmbedBuilder embedBuilder = null);
    }
}
