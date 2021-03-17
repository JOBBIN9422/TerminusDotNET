using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Attributes;

namespace TerminusDotNetCore.NamedArgs
{
    [NamedArgument]
    [NamedArgumentType]
    public class RadioAddSongArgs
    {
        [Description("Name of playlist to add song to.")]
        public string Playlist { get; set; }
        [Description("YouTube URL of song to add to playlist.")]
        public string Url { get; set; }
    }
}
