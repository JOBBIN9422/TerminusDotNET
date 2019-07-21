using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetConsoleApp.Modules;

namespace TerminusDotNetConsoleApp.Services
{
    public interface ICustomService
    {
        IServiceModule ParentModule { get; set; }

        Task ServiceReplyAsync(string s, EmbedBuilder embedBuilder = null);
    }
}
