using Discord;
using Discord.Commands;
using Discord.WebSocket;
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

        [Command("usersim", RunMode = RunMode.Async)]
        [Summary("Generate a sentence based on a user's messages in a given channel.")]
        public async Task GenerateUserSentence([Summary("@user to generate the sentence for.")]IUser user, [Summary("#channel to pull user messages from.")]ISocketMessageChannel channel)
        {
            string userSentence = await _markovService.GenerateUserSentence(user, channel);
            await ReplyAsync(userSentence);
        }

    }
}
