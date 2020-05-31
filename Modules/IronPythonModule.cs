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
            _pythonService = pythonService;
            _pythonService.Config = config;
            _pythonService.ParentModule = this;
        }

        [Command("python", RunMode = RunMode.Async)]
        public async Task ExecutePythonString([Remainder]string pythonStr)
        {
            List<string> pythonOut = _pythonService.ExecutePythonString(pythonStr);

            foreach (string outPage in pythonOut)
            {
                await ReplyAsync($"```\n{outPage}\n```");
            }
        }
    }
}
