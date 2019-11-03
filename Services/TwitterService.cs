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
        public IServiceModule ParentModule { get; set; }
        private TwitterContext _twitterContext;
        private Random _random = new Random();

        public TwitterService()
        {
            IConfiguration config = new ConfigurationBuilder()
                                        .AddJsonFile("appsettings.json", true, true)
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
