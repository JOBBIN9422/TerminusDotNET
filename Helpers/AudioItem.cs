
using Discord;
using Discord.Commands;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Discord.WebSocket;

namespace TerminusDotNetCore.Helpers
{
    
    
    public abstract class AudioItem
    {
        //the local path to the audio file
        public string Path { get; set; }

        //a human-readable name (may be different from its file path)
        public string DisplayName { get; set; }
        
        //channel ID to play this item in
        public ulong PlayChannelId { get; set; }

        //the person who added this item to the queue
        public string OwnerName { get; set; }

        //the time that playback of this item started
        public DateTime StartTime { get; set; }
    }
}
