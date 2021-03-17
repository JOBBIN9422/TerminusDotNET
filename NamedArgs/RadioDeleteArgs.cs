﻿using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetCore.NamedArgs
{
    [NamedArgumentType]
    public class RadioDeleteArgs
    {
        public string Playlist { get; set; }
        public int Index { get; set; } = -1;
    }
}
