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
    public class AudioEventCreateArgs
    {
        [Description("The aliased song name to play on schedule (add with `!alias` if needed).")]
        public string SongName { get; set; }

        [Description("Cron string used to set the event's schedule.")]
        public string Cron { get; set; }

        [Description("Name of channel to play the song in (`main` or `weed`).")]
        public string ChannelName { get; set; } = "main";

    }
}
