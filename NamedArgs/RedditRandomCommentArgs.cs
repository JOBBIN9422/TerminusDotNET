using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Attributes;

namespace TerminusDotNetCore.NamedArgs
{
    public class RedditRandomCommentArgs
    {
        [Description("The name of the subreddit to pull a random comment from.")]
        public string Sub { get; set; }

        [Description("Type of comment to sort by.")]
        public string SortBy { get; set; }

    }
}
