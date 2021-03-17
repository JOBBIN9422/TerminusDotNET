using Discord.Commands;
using Discord.WebSocket;
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
    public class RadioAccessControlArgs
    {
        [Description("Playlist name to whitelist/blacklist user for.")]
        public string Playlist { get; set; }
        [Description("`@user` mention to whitelist/blacklist for the given playlist.")]
        public SocketUser User { get; set; } = null;
    }
}
