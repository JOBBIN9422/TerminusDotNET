using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetCore.NamedArgs
{
    public class RadioAccessControlArgs
    {
        public string Playlist { get; set; }
        public SocketUser User { get; set; } = null;
    }
}
