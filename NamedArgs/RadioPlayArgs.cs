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
        public string Playlist { get; set; }
        public bool Append { get; set; } = true;
        public bool Shuffle { get; set; } = false;
    }
}
