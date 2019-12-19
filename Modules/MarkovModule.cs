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
    public class MarkovModule : ServiceControlModule
    {
        private MarkovService _markovService;

        public MarkovModule(MarkovService service)
        {
            _markovService = service;
            _markovService.ParentModule = this;
        }

        [Command("clickbait", RunMode = RunMode.Async)]
        [Summary("Generate a random clickbait article title.")]
        public async Task GenerateClickbaitSentence()
        {
            string clickbaitTitle = _markovService.GenerateClickbaitSentence();
	    await ServiceReplyAsync(clickbaitTitle);
        }

    }
}
