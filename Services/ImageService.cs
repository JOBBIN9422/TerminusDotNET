using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using TerminusDotNetConsoleApp.Modules;

namespace TerminusDotNetConsoleApp.Services
{
    public class ImageService : ICustomService
    {
        public IServiceModule ParentModule { get; set; }

        public async Task ServiceReplyAsync(string s, EmbedBuilder embedBuilder = null)
        {
            if (embedBuilder == null)
            {
                await ParentModule.ServiceReplyAsync(s);
            }
            else
            {
                await ParentModule.ServiceReplyAsync(s, embedBuilder);
            }  
        }
    }
}
