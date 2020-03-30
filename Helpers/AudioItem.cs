
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
    
    
    public abstract class AudioItem
    {
        //for local files: the filename. for streamed audio: the youtube URL.
        public string Path { get; set; }

        //a human-readable name (may be different from its file path)
        public string DisplayName { get; set; }
        
        //channel ID to play this item in
        public ulong PlayChannelId { get; set; }
    }
}
