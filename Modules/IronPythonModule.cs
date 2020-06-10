using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Services;
using TerminusDotNetCore.Helpers;

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
        public async Task ExecutePythonString([Remainder]string pythonStr = null)
        {
            List<string> pythonOut = new List<string>();

            //check for python files in the current message
            if (pythonStr == null &&
                Context.Message.Attachments != null &&
                Context.Message.Attachments.Count > 0 &&
                AttachmentHelper.AttachmentsAreValid(Context.Message.Attachments, AttachmentFilter.Plaintext))
            {
                pythonOut = _pythonService.ExecutePythonFiles(Context.Message.Attachments);
            }

            //otherwise, use the given text
            else
            {
                pythonOut = _pythonService.ExecutePythonString(pythonStr);
            }

            //sent output as monospaced text
            foreach (string outPage in pythonOut)
            {
                await ReplyAsync($"```\n{outPage}\n```");
            }
        }
    }
}
