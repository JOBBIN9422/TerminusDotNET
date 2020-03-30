using System;
using System.Collections.Generic;
using System.Text;

namespace TerminusDotNetCore.Helpers
{
    public class YouTubeAudioItem : AudioItem
    {
        public string VideoUrl { get; set; }
        public YouTubeAudioType AudioSource { get; set; }
    }
}
