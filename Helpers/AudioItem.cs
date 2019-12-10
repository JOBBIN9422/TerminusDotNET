
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
    public enum AudioType
    {
        Local,
        YouTube
    }
    
    public class AudioItem
    {
        //for local files: the filename. for streamed audio: the youtube URL.
        public string Path { get; set; }
        
        //channel ID to play this item in
        public ulong PlayChannelId { get; set; }
        
        //where does the audio originate? (local file or streamed from youtube)
        public AudioType AudioSource { get; set; }
    }
}
