using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Services;

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

        [Command("okboomer", RunMode = RunMode.Async)]
        public async Task GetBoomerTweet()
        {
            string tweet = await _twitterService.SearchTweetRandom("\"boomer memes\" OR \"boomer meme\" OR \"boomer quotes\" OR milennial OR milennials OR genz OR \"gen z\" OR genz OR zoomer");
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
