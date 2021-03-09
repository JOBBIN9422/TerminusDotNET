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
        private static async Task<string> RunBashCommand(string cmd, string user)
        {
            //escape double quotes in cmd
            string escapedCmd = cmd.Replace("\"", "\\\"");
            Console.WriteLine(cmd);
            using (var bashProcess = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"-u {user} \"{cmd}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                }))
            {
                Console.WriteLine(bashProcess.StartInfo.Arguments);
                string stdError = await bashProcess.StandardError.ReadToEndAsync();
                if (!string.IsNullOrEmpty(stdError))
                {
                    return stdError;
                }
                return await bashProcess.StandardOutput.ReadToEndAsync();
            }
        }

        public async Task RunBashCommandNonRoot(string cmd)
        {
            string bashOutput = await RunBashCommand(cmd, "termy");
            await ParentModule.ServiceReplyAsync($"```\n{bashOutput}\n```");
        }

        public async Task ShowNeofetchOutput()
        {
            string neofetchOutput = await RunBashCommand("neofetch --stdout", "root");
            await ParentModule.ServiceReplyAsync($"```\n{neofetchOutput}\n```");
        }

        public async Task RunAptUpdate()
        {
            string aptOutput = await RunBashCommand("apt update", "root");
            await ParentModule.ServiceReplyAsync($"```\n{aptOutput}\n```");
        }

        public async Task RunAptFullUpgrade()
        {
            string aptOutput = await RunBashCommand("apt full-upgrade -y", "root");
            await ParentModule.ServiceReplyAsync($"```\n{aptOutput}\n```");
        }

        public async Task RunAptCleanAndRemove()
        {
            string aptOutput = await RunBashCommand("apt autoremove -y && apt autoclean", "root");
            await ParentModule.ServiceReplyAsync($"```\n{aptOutput}\n```");
        }
    }   
}
