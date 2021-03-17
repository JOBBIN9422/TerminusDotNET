using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using TerminusDotNetCore.Attributes;

namespace TerminusDotNetCore.NamedArgs
{
    [NamedArgument]
    [NamedArgumentType]
    public class AudioQueueArgs
    {
        //true: enqueue at end (normal behavior)
        //false: enqueue at front (cut in queue)
        [Description("Append the song or playlist if `true`, insert the song or playlist at the front of the queue if `false`.")]
        public bool Append { get; set; } = true;
        [Description("Shuffle the playlist if `true`, preserve playlist order if `false`.")]
        public bool Shuffle { get; set; } = false;

        //channel alias ("main" or "weed")
        [Description("Channel to play in (`main` or `weed`).")]
        public string Channel { get; set; } = "main";
    }
}
