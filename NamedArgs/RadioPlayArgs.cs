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
    public class RadioPlayArgs
    {
        [Description("Name of the playlist to add to queue.")]
        public string Playlist { get; set; }
        [Description("Add playlist to end of queue if `true`, add playlist to front of queue if `false`.")]
        public bool Append { get; set; } = true;
        [Description("Shuffle the playlist if `true`, preserve playlist order if `false`.")]
        public bool Shuffle { get; set; } = false;
    }
}
