using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Services;

namespace TerminusDotNetCore.Modules
{
    public class RedditModule : ServiceControlModule
    {
        private RedditService _service;
        public RedditModule(IConfiguration config, RedditService service) : base(config)
        {
            _service = service;
            _service.ParentModule = this;
        }


        [Command("reddit-moment", RunMode = RunMode.Async)]
        public async Task TestRedditApi()
        {
            await _service.TestRedditApi();
        }
    }
}
