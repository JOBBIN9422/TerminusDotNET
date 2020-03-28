using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
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

        public MarkovModule(IConfiguration config, MarkovService service) : base(config)
        {
            _markovService = service;
            _markovService.Config = config;
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
            if (user == null || channel == null)
            {
                await ServiceReplyAsync("Please provide both a @user and a #channel.");
                return;
            }

            string userSentence = await _markovService.GenerateUserSentence(user, channel);
            await ServiceReplyAsync(userSentence);
        }

        [Command("channelsim", RunMode = RunMode.Async)]
        [Summary("Generate a sentence based on a given channel.")]
        public async Task GenerateChannelSentence([Summary("#channel to pull user messages from.")]ISocketMessageChannel channel)
        {
            if (channel == null)
            {
                await ServiceReplyAsync("Please provide a #channel.");
                return;
            }

            string channelSentence = await _markovService.GenerateChannelSentence(channel);
            await ServiceReplyAsync(channelSentence);
        }
    }
}
