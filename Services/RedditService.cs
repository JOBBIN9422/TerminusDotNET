using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Modules;
using Reddit;
using Reddit.Controllers;

namespace TerminusDotNetCore.Services
{
    public class RedditService : ICustomService
    {
        public IConfiguration Config { get; set; }
        public ServiceControlModule ParentModule { get; set; }
        private RedditClient _redditClient;

        public RedditService(IConfiguration config)
        {
            Config = config;
            _redditClient = new RedditClient(appId: Config["RedditClientId"], appSecret: Config["RedditClientSecret"]);
        }

        public async Task TestRedditApi()
        {
            Random rand = new Random();
            List<Post> frontPagePosts = _redditClient.FrontPage;
            await ParentModule.ServiceReplyAsync(frontPagePosts[rand.Next(frontPagePosts.Count)].Title);
        }
    }

}
