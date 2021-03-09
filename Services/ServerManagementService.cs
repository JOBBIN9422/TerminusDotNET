using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Modules;

namespace TerminusDotNetCore.Services
{
    public class ServerManagementService : ICustomService
    {
        public IConfiguration Config { get; set; }
        public ServiceControlModule ParentModule { get; set; }

        //don't pass anything suspect into here, you're running as root :-]
        private static async Task<string> RunBashCommand(string cmd)
        {
            using (var bashProcess = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"{cmd}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                }))
            {
                return await bashProcess.StandardOutput.ReadToEndAsync();
            }
        }

        public async Task ShowNeofetchOutput()
        {
            string neofetchOutput = await RunBashCommand("neofetch --stdout");
            await ParentModule.ServiceReplyAsync($"```\n{neofetchOutput}\n```");
        }

        public async Task UpdatePackages()
        {
            string aptOutput = await RunBashCommand("apt update && apt full-upgrade -y");
            await ParentModule.ServiceReplyAsync($"```\n{aptOutput}\n```");
        }
    }   
}
