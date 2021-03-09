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
    public class ServerManagementModule : ServiceControlModule
    {
        private ServerManagementService _service;
        public ServerManagementModule(IConfiguration config, ServerManagementService service) : base(config)
        {
            _service.Config = config;
            _service = service;
            _service.ParentModule = this;
        }

        [Command("neofetch", RunMode = RunMode.Async)]
        [Summary("Run `neofetch`.")]
        public async Task RunNeofetch()
        {
            await _service.ShowNeofetchOutput();
        }
    }
}
