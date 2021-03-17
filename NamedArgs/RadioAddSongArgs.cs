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
        public string Playlist { get; set; }
        public string Url { get; set; }
    }
}
