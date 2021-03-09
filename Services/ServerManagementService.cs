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

        public List<ulong> RootUserIds { get; private set; } = new List<ulong>();

        public void LoadRootUserIds()
        {
            var rootUserIds = Config.GetSection("ShellRootUsers").GetChildren();
            foreach (var userIdSection in rootUserIds)
            {
                Console.WriteLine(userIdSection.Value);
                RootUserIds.Add(ulong.Parse(userIdSection.Value));
            }
        }
        private static async Task<string> RunBashCommand(string cmd, string user)
        {
            //escape double quotes in cmd
            string escapedCmd = cmd.Replace("\"", "\\\"");
            using (var bashProcess = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "su",
                    Arguments = $"-s /bin/bash -c \"{escapedCmd}\" {user}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                }))
            {
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

        public async Task RunBashCommandRoot(string cmd, ulong userID)
        {
            Console.WriteLine(userID);
            //check if user has root permissions (set in config)
            if (!RootUserIds.Contains(userID))
            {
                await ParentModule.ServiceReplyAsync("You don't have root access.");
                return;
            }

            string bashOutput = await RunBashCommand(cmd, "root");
            await ParentModule.ServiceReplyAsync($"```\n{bashOutput}\n```");
        }

        public async Task ShowNeofetchOutput()
        {
            string neofetchOutput = await RunBashCommand("neofetch --stdout", "root");
            await ParentModule.ServiceReplyAsync($"```\n{neofetchOutput}\n```");
        }

        public async Task RunAptUpdate()
        {
            string aptOutput = await RunBashCommand("apt-get update", "root");
            await ParentModule.ServiceReplyAsync($"```\n{aptOutput}\n```");
        }

        public async Task RunAptFullUpgrade()
        {
            string aptOutput = await RunBashCommand("apt-get dist-upgrade -y", "root");
            await ParentModule.ServiceReplyAsync($"```\n{aptOutput}\n```");
        }

        public async Task RunAptCleanAndRemove()
        {
            string aptOutput = await RunBashCommand("apt-get autoremove -y && apt-get autoclean", "root");
            await ParentModule.ServiceReplyAsync($"```\n{aptOutput}\n```");
        }
    }   
}
