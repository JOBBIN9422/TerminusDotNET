using System.IO;
using System.Linq;
using System.Collections.Generic;
using TerminusDotNetCore.Modules;
using TerminusDotNetCore.Helpers;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;

namespace TerminusDotNetCore.Services
{
    public class MarkovService : ICustomService
    {
        public ServiceControlModule ParentModule { get; set; }
        private MarkovHelper _clickbaitMarkov = new MarkovHelper(Path.Combine("assets", "clickbait.txt"));

        public string GenerateClickbaitSentence()
        {
            return _clickbaitMarkov.GenerateSentence();
        }

        public async Task<string> GenerateUserSentence(IUser user, ISocketMessageChannel channel)
        {
            var messages = await channel.GetMessagesAsync(1000, CacheMode.AllowDownload).FlattenAsync();
            var userMessages = messages.Where(msg => msg.Author == user);
            List<string> userMessagesContent = new List<string>();
            foreach (var message in userMessages)
            {
                if (message.Author == user)
                {
                    userMessagesContent.Add(message.Content);
                }
            }

            MarkovHelper userMarkov = new MarkovHelper(userMessagesContent);

            return userMarkov.GenerateSentence();
        }
    }
}
