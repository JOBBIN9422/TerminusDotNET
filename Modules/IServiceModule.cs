using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetConsoleApp.Modules
{
    public interface IServiceModule
    {
        //allow services to reply on a text channel
        Task ServiceReplyAsync(string s);
        Task ServiceReplyAsync(string title, EmbedBuilder embedBuilder);
    }
}
