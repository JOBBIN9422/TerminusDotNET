
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
    public class AudioItem
    {
        public string Path { get; set; }
        public ulong PlayChannelId { get; set; }
    }
}
