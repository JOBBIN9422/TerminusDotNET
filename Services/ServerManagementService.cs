﻿using Microsoft.Extensions.Configuration;
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
                }))
            {
                return await bashProcess.StandardOutput.ReadToEndAsync();
            }
        }

        public async Task ShowNeofetchOutput()
        {
            string neofetchOutput = await RunBashCommand("neofetch");
            await ParentModule.ServiceReplyAsync($"```\n{neofetchOutput}\n```");
        }
    }   
}