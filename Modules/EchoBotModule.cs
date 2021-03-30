using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TerminusDotNetCore.Modules
{
    public class EchoBotModule : ModuleBase<SocketCommandContext>
    {
        public IConfiguration Config { get; set; }

        public EchoBotModule(IConfiguration config)
        {
            Config = config;
        }

        [Command("echo-bot", RunMode = RunMode.Async)]
        [Summary("Activates the Echo Bot")]
        public async Task EchoBot([Summary("Which branch of the Echo Bot repo to pull")] string branch="master")
        {
            // Hopefully this will set the right environment variables
            string lib_dir = Config["EchoBotDir"];
            System.Environment.SetEnvironmentVariable("ECHOBOT_DIR", lib_dir);
            System.Environment.SetEnvironmentVariable("ECHOBOT_BRANCH", branch);

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "Automation/activate_echo_bot.sh";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;

            using (Process p = Process.Start(psi))
            {
                string strOutput = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }
            await ReplyAsync("Echo Bot Activated");
        }

        [Command("kill_echo_bot", RunMode = RunMode.Async)]
        [Summary("Decommissions Echo Bot.... For Now")]
        public async Task KillEchoBot()
        {
            // In the future, we may have persistent data to be saved from the docker container
            // If that happens, we should first copy that data out of the container and into
            // the persistent_files dir. This could happen here or in the deactivate script.
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "Automation/deactivate_echo_bot.sh";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;

            using (Process p = Process.Start(psi))
            {
                string strOutput = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }
            await ReplyAsync("Echo Bot Deactivated");
        }
    }
}