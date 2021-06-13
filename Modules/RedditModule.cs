using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.NamedArgs;
using TerminusDotNetCore.Services;

namespace TerminusDotNetCore.Modules
{
    public class RedditModule : ServiceControlModule
    {
        private RedditService _service;
        public RedditModule(IConfiguration config, RedditService service) : base(config)
        {
            _service = service;
            _service.ParentModule = this;
        }

        private static RedditRandomCommentArgs DEFAULT_PORN_REVIEW_ARGS = new RedditRandomCommentArgs
        {
            Sub = "NSFW",
            SortBy = "best"
        };


        [Command("reddit-moment", RunMode = RunMode.Async)]
        public async Task TestRedditApi()
        {
            await _service.TestRedditApi();
        }

        public async Task PornReviewSerious(RedditRandomCommentArgs args = null)
        {
            if (args == null)
            {
                args = DEFAULT_PORN_REVIEW_ARGS;
            }

            await _service.GetRandomPornComment(args.Sub, args.SortBy);
        }
    }
}
