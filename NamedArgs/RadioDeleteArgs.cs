using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetCore.NamedArgs
{
    public class RadioDeleteArgs
    {
        public string Playlist { get; set; }
        public int Index { get; set; } = -1;
    }
}
