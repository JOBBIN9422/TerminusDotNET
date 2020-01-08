using System;
using System.Collections.Generic;
using System.Text;
using TerminusDotNetCore.Modules;
using LinqToTwitter;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace TerminusDotNetCore.Services
{
    public class TwitterService : ICustomService
    {
        public ServiceControlModule ParentModule { get; set; }
        private TwitterContext _twitterContext;
        private Random _random = new Random();

        public TwitterService()
        {
            IConfiguration config = new ConfigurationBuilder()
                                        .AddJsonFile("secrets.json", true, true)
                                        .Build();

            string consumerKey = config["TwitterConsumerKey"];
            string consumerSecret = config["TwitterConsumerSecret"];
            string token = config["TwitterAccessToken"];
            string tokenSecret = config["TwitterAccessTokenSecret"];

            IAuthorizer auth = new SingleUserAuthorizer
            {
                CredentialStore = new SingleUserInMemoryCredentialStore
                {
                    ConsumerKey = consumerKey,
                    ConsumerSecret = consumerSecret,
                    AccessToken = token,
                    AccessTokenSecret = tokenSecret
                }
            };

            Task.Run(async () => await InitTwitterAPIAsync(auth));
        }

        private async Task InitTwitterAPIAsync(IAuthorizer auth)
        {
            await auth.AuthorizeAsync();
            _twitterContext = new TwitterContext(auth);
        }
        
        public async Task<string> TweetAsync(string tweetContent)
        {
            if (!string.IsNullOrEmpty(tweetContent))
            {
                var tweet = await _twitterContext.TweetAsync(tweetContent);
                if (tweet != null)
                {
                    return $"Successfully tweeted status:  https://twitter.com/Yeetman04889000/status/{tweet.StatusID}";
                }
                else
                {
                    return "An error occurred while attempting to post the tweet.";
                }
            }
            else
            {
                return "No tweet content was provided.";
            }
        }
        
        public async Task<string> GetLastNotchTweet()
        {
            var user =
                await
                (from tweet in _twitterContext.User
                 where tweet.Type == UserType.Show &&
                       tweet.ScreenName == "notch"
                 select tweet)
                .SingleOrDefaultAsync();

            if (user != null)
            {
                var name = user.ScreenNameResponse;
                var lastStatus =
                    user.Status == null ? "No recent tweet(s) found." : user.Status.Text;
                return lastStatus;
            }
            return "No user found.";
        }
        
        public async Task<string> SearchTweetRandom(string searchTerm)
        {
            List<Search> userQuery = new List<Search>();
            try
            {
                userQuery = await (
                from search in _twitterContext.Search
                where search.Type == SearchType.Search &&
                      search.Query == searchTerm &&
                      search.IncludeEntities == true &&
                      search.TweetMode == TweetMode.Extended &&
                      search.SearchLanguage == "en"
                select search
                ).ToListAsync();
            }
            catch (Exception e)
            {
                return e.ToString();
            }

            string returnStr = string.Empty;
            if (userQuery != null)
            {
                var statuses = userQuery[_random.Next(0, userQuery.Count)].Statuses;
                int statusIndex = _random.Next(0, statuses.Count);
                returnStr = statuses[statusIndex].Text ?? statuses[statusIndex].FullText ?? string.Empty;
            }

            return returnStr;
        }
    }
}
