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
            _service = service;
            _service.Config = config;
            _service.ParentModule = this;
        }

        [Command("neofetch", RunMode = RunMode.Async)]
        [Summary("Run `neofetch`.")]
        public async Task RunNeofetch()
        {
            await _service.ShowNeofetchOutput();
        }

        

        [Group("apt")]
        public class AptModule : ServiceControlModule
        {
            private ServerManagementService _service;

            public AptModule(IConfiguration config, ServerManagementService service) : base(config)
            {
                //do not need to set service config here - passed into audioSvc constructor via DI
                _service = service;
                _service.Config = config;
                _service.ParentModule = this;
            }

            [Command("update", RunMode = RunMode.Async)]
            [Summary("Run `apt update`.")]
            public async Task UpdatePackages()
            {
                await _service.RunAptUpdate();
            }

            [Command("full-upgrade", RunMode = RunMode.Async)]
            [Summary("Run `apt full-upgrade -y`.")]
            public async Task FullUpgradePackages()
            {
                await _service.RunAptFullUpgrade();
            }

            [Command("clean", RunMode = RunMode.Async)]
            [Summary("Run `apt autoremove -y && apt autoclean`.")]
            public async Task CleanPackages()
            {
                await _service.RunAptCleanAndRemove();
            }
        }
    }
}
