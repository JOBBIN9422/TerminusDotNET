using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Services;

namespace TerminusDotNetCore.Modules
{
    public class IronPythonModule : ServiceControlModule
    {
        private IronPythonService _pythonService;
        public IronPythonModule(IConfiguration config, IronPythonService pythonService) : base(config)
        {
            //no need to set config (set in constructor via DI)
            _pythonService = pythonService;
            _pythonService.ParentModule = this;
        }

        [Command("python", RunMode = RunMode.Async)]
        public async Task ExecutePythonString([Remainder]string pythonStr)
        {
            string pythonOut = _pythonService.ExecutePythonString(pythonStr);

            await ReplyAsync($"```\n{pythonOut}\n```");
        }
    }
}
