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
    public class RadioDeleteArgs
    {
        [Description("Name of playlist to delete OR delete song from.")]
        public string Playlist { get; set; }
        [Description("Index of song to delete from playlist (use `!radio list <name>` to find). If not provided, **delete the entire playlist**.")]
        public int Index { get; set; } = -1;
    }
}
