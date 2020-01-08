using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Services;
using System.Linq;

namespace TerminusDotNetCore.Modules
{
    public class TwitterModule : ServiceControlModule
    {
        private TwitterService _twitterService;

        public TwitterModule(TwitterService service)
        {
            _twitterService = service;
            _twitterService.ParentModule = this;
        }

        [Command("twitter", RunMode = RunMode.Async)]
        [Summary("Search Twitter for recent tweets based on the given search term.")]
        public async Task SearchTweets([Summary("The term(s) to search for. Use quotes for terms with spaces. You can use logical connectors (e.g. AND/OR) to build more complex searches.")][Remainder]string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                await ReplyAsync("Please supply a search term.");
            }
            string tweet = await _twitterService.SearchTweetRandom(searchTerm);
            try
            {
                await ReplyAsync(tweet);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        [Command("tweet", RunMode = RunMode.Async)]
        public async Task Tweet([Remainder]string tweet = null)
        {
            string result = "";
            
            if (string.IsNullOrEmpty(tweet))
            {
                //check if the previous message has any text
                var messages = await Context.Channel.GetMessagesAsync(2).FlattenAsync();
                var priorMessage = messages.Last();
                if (!string.IsNullOrEmpty(priorMessage.Content))
                {
                    result = await _twitterService.TweetAsync(priorMessage.Content);
                }
            }
            else
            {
                result = await _twitterService.TweetAsync(tweet);
            }
            await ReplyAsync(result);
        }

        [Command("notch", RunMode = RunMode.Async)]
        public async Task GetLastNotchTweet()
        {
            string tweet = await _twitterService.GetLastNotchTweet();
            try
            {
                await ReplyAsync(tweet);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        [Command("copypasta", RunMode = RunMode.Async)]
        public async Task GetCopypasta()
        {
            string tweet = await _twitterService.SearchTweetRandom("copypasta");
            try
            {
                await ReplyAsync(tweet);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }
}
