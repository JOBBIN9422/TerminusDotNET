using Discord.Commands;
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

        public LibertyModule(Iconfiguration config)
        {
            Config = config;
        }

        [Command("liberty")]
        [Summary("Activates the Liberty Prime Bot")]
        public async Task Echo([Summary("Which branch of the liberty repo to pull")] string branch="master")
        {
            // Hopefully this will set the right environment variables
            string lib_dir = Config["LibertyPrimeDir"]
            System.Environment.SetEnvironmentVariable("LIBERTY_DIR",lib_dir)
            System.Environment.SetEnvironmentVariable("LIBERTY_BRANCH", branch)
            ProcessStartInfo psi = new ProcessStartInfo();
            // Need to make sure this points to the right spot. Is the root of the repo the working directory?
            psi.FileName = "Automation/activate_liberty_prime.sh";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;

            Process p = Process.Start(psi);
            string strOutput = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            await ReplyAsync("It either worked or it didn't");
        }
    }
}
