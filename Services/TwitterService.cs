using System;
using System.Collections.Generic;
using System.Text;
using TerminusDotNetCore.Modules;
using TerminusDotNetCore.Helpers;
using LinqToTwitter;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using Discord;
using System.IO;

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
        
        private async Task<List<Media>> GetMediaFromAttachments(IReadOnlyCollection<IAttachment> attachments)
        {
            if (attachments == null || attachments.Count == 0)
            {
                return null;
            }

            List<Media> mediaContent = new List<Media>();
            var attachmentFiles = AttachmentHelper.DownloadAttachments(attachments);

            try
            {
                foreach (var file in attachmentFiles)
                {
                    string mediaType = $"image/{Path.GetExtension(file).Replace(".", string.Empty)}";
                    byte[] fileData = File.ReadAllBytes(file);
                    var media = await _twitterContext.UploadMediaAsync(fileData, mediaType, "tweet_image");

                    mediaContent.Add(media);
                    File.Delete(file);
                }

                return mediaContent;
            }
            finally
            {
                AttachmentHelper.DeleteFiles(attachmentFiles);
            }
        }

        public async Task<string> TweetAsync(string tweetContent, IReadOnlyCollection<IAttachment> attachments = null)
        {
            Status tweet = null;

            //get media from message attachments (if any)
            List<Media> mediaContent = await GetMediaFromAttachments(attachments);
            if (mediaContent != null)
            {
                var mediaIDs = mediaContent.Select(x => x.MediaID);
                tweet = await _twitterContext.TweetAsync(tweetContent, mediaIDs);

                //if (!string.IsNullOrEmpty(tweetContent))
                //{
                //    tweet = await _twitterContext.TweetAsync(tweetContent, mediaIDs);
                //}
                //else
                //{
                //    tweet = await _twitterContext.TweetAsync("", mediaIDs);
                //}
            }
            else
            {
                //if no media provided, just tweet the text 
                tweet = await _twitterContext.TweetAsync(tweetContent);
            }
            
            if (tweet != null)
            {
                return $"Successfully tweeted status:  https://twitter.com/Yeetman04889000/status/{tweet.StatusID}";
            }
            else
            {
                return "An error occurred while attempting to post the tweet.";
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
