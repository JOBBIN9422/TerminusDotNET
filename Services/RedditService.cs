using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Modules;
using Reddit;
using Reddit.Controllers;
using System.Net.Http;
using System.Net.Http.Headers;

namespace TerminusDotNetCore.Services
{
    public class RedditService : ICustomService
    {
        public IConfiguration Config { get; set; }
        public ServiceControlModule ParentModule { get; set; }
        private RedditClient _redditClient;

        private Random _random;

        public RedditService(IConfiguration config, Random random)
        {
            Config = config;
            _redditClient = new RedditClient(appId: Config["RedditClientId"], appSecret: Config["RedditClientSecret"], refreshToken: Config["RedditRefreshToken"]);
            _random = random;
        }

        public async Task TestRedditApi()
        {
            Random rand = new Random();
            List<Post> frontPagePosts = _redditClient.FrontPage;
            await ParentModule.ServiceReplyAsync(frontPagePosts[rand.Next(frontPagePosts.Count)].Title);
        }

        public async Task GetRandomPornComment(string sub, string sortBy)
        {
            Subreddit subreddit = _redditClient.Subreddit(sub);
            List<Post> posts = new List<Post>();

            switch (sortBy.ToLower())
            {
                case "new":
                    posts = subreddit.Posts.GetNew();
                    break;
                case "top":
                    posts = subreddit.Posts.GetTop();
                    break;
                case "controversial":
                    posts = subreddit.Posts.GetControversial();
                    break;
                case "rising":
                    posts = subreddit.Posts.GetRising();
                    break;
                case "hot":
                    posts = subreddit.Posts.GetHot();
                    break;
            }

            Post randomPost = posts.ElementAt(_random.Next(posts.Count));
            List<Comment> randomComments = randomPost.Comments.GetRandom();
            Comment randomComment = randomComments.ElementAt(_random.Next(randomComments.Count));
            await ParentModule.ServiceReplyAsync($"\"{randomComment.Body}\" - /u/{randomComment.Author}");
        }
    }

}
