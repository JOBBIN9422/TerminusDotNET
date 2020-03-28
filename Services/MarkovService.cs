using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using TerminusDotNetCore.Modules;
using TerminusDotNetCore.Helpers;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.Configuration;

namespace TerminusDotNetCore.Services
{
    public class MarkovService : ICustomService
    {
        public IConfiguration Config { get; set; }
        public ServiceControlModule ParentModule { get; set; }
        private MarkovHelper _clickbaitMarkov = new MarkovHelper(Path.Combine("assets", "clickbait.txt"));

        public string GenerateClickbaitSentence()
        {
            return _clickbaitMarkov.GenerateSentence();
        }

        public async Task<string> GenerateUserSentence(IUser user, ISocketMessageChannel channel)
        {
            var messages = await channel.GetMessagesAsync(2000, CacheMode.AllowDownload).FlattenAsync();
            var userMessages = messages.Where(msg => msg.Author == user);
            List<string> userMessagesContent = new List<string>();
            foreach (var message in userMessages)
            {
                //ignore commands
                if (message.Author == user && !Regex.IsMatch(message.Content, @"\!\w"))
                {
                    //strip emotes
                    //string messageNoEmotes = Regex.Replace(message.Content, @"\<:\w+:\d+\>", "");
                    userMessagesContent.Add(message.Content);
                }
            }

            MarkovHelper userMarkov = new MarkovHelper(userMessagesContent);

            return userMarkov.GenerateSentence();
        }

        public async Task<string> GenerateChannelSentence(ISocketMessageChannel channel)
        {
            var messages = await channel.GetMessagesAsync(2000, CacheMode.AllowDownload).FlattenAsync();
            List<string> messagesContent = new List<string>();

            foreach (var message in messages)
            {
                messagesContent.Add(message.Content);
            }

            MarkovHelper markov = new MarkovHelper(messagesContent);
            return markov.GenerateSentence();
        }
    }
}
