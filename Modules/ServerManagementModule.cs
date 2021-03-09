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

            //read root user IDs from config
            _service.LoadRootUserIds(false);
        }

        [Command("neofetch", RunMode = RunMode.Async)]
        [Summary("Run `neofetch`.")]
        public async Task RunNeofetch()
        {
            await _service.ShowNeofetchOutput();
        }

        [Command("bash", RunMode = RunMode.Async)]
        [Summary("Run the given `bash` command as a **non-root** user.")]
        public async Task RunBashCommandNonRoot([Summary("Command to run.")][Remainder]string cmd)
        {
            await _service.RunBashCommandNonRoot(cmd);
        }

        [Command("root", RunMode = RunMode.Async)]
        [Summary("Run the given `bash` command as a **root** user (must be authorized in `appsettings.json`). Be responsible pls :')")]
        public async Task RunBashCommandRoot([Summary("Command to run.")][Remainder] string cmd)
        {
            await _service.RunBashCommandRoot(cmd, Context.Message.Author.Id);
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
            [Summary("Run `apt-get update`.")]
            public async Task UpdatePackages()
            {
                await _service.RunAptUpdate();
            }

            [Command("dist-upgrade", RunMode = RunMode.Async)]
            [Summary("Run `apt-get dist-upgrade -y`.")]
            public async Task FullUpgradePackages()
            {
                await _service.RunAptFullUpgrade();
            }

            [Command("clean", RunMode = RunMode.Async)]
            [Summary("Run `apt-get autoremove -y && apt-get autoclean`.")]
            public async Task CleanPackages()
            {
                await _service.RunAptCleanAndRemove();
            }
        }
    }
}
