using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TerminusDotNetCore.Modules
{
    public class LibertyModule : ModuleBase<SocketCommandContext>
    {

        public IConfiguration Config { get; set; }

        public LibertyModule(IConfiguration config)
        {
            Config = config;
        }

        [Command("liberty")]
        [Summary("Activates the Liberty Prime Bot")]
        public async Task Liberty([Summary("Which branch of the liberty repo to pull")] string branch="master")
        {
            // Hopefully this will set the right environment variables
            string lib_dir = Config["LibertyPrimeDir"];
            System.Environment.SetEnvironmentVariable("LIBERTY_DIR",lib_dir);
            System.Environment.SetEnvironmentVariable("LIBERTY_BRANCH", branch);

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "Automation/activate_liberty_prime.sh";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;

            Process p = Process.Start(psi);
            string strOutput = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            await ReplyAsync("Liberty Prime Activated");
        }

        [Command("kill_liberty")]
        [Summary("Decommissions Liberty Prime.... For Now")]
        public async Task KillLiberty()
        {
            // In the future, we may have persistent data to be saved from the docker container
            // If that happens, we should first copy that data out of the container and into
            // the persistent_files dir. This could happen here or in the deactivate script.
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "Automation/deactivate_liberty_prime.sh";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;

            Process p = Process.Start(psi);
            string strOutput = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            await ReplyAsync("Liberty Prime Deactivated");
        }
    }
}
