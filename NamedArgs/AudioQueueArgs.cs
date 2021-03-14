using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace TerminusDotNetCore.NamedArgs
{
    [NamedArgumentType]
    public class AudioQueueArgs
    {
        //true: enqueue at end (normal behavior)
        //false: enqueue at front (cut in queue)
        public bool Append { get; set; } = true;
        public bool Shuffle { get; set; } = false;

        //channel alias ("main" or "weed")
        public string Channel { get; set; } = "main";
    }
}
