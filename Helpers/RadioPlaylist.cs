using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace TerminusDotNetCore.Helpers
{
    public class RadioPlaylist
    {
        public string OwnerName { get; set; }
        public List<string> WhitelistUsers { get; set; }

        public LinkedList<YouTubeAudioItem> Songs { get; set; }
    }
}
