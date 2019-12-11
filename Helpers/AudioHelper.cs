using Discord;
using Discord.Commands;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace TerminusDotNetCore.Helpers
{
    public class AudioHelper
    {
        public static Process CreateYTStreamProcess(string path)
        {
            //idk if this is going to work
            return Process.Start(new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"youtube-dl -o - \"[{path}]\" | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
        }
    }
}
