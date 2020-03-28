using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using TerminusDotNetCore.Services;

namespace TerminusDotNetCore.Modules
{
    public class TextEditModule : ServiceControlModule
    {
        private TextEditService _textEditService;

        public TextEditModule(IConfiguration config, IConfiguration secrets, TextEditService service) : base(config, secrets)
        {
            _textEditService = service;
            _textEditService.Config = config;
            _textEditService.ParentModule = this;
        }

        [Command("wide", RunMode = RunMode.Async)]
        [Summary("Converts the input message (or the contents of the previous message) into full-width text.")]
        public async Task ConvertMessageToFullWidthAsync([Summary("the message to convert")][Remainder]string message = null)
        {
            string wideText = string.Empty;
            if (string.IsNullOrEmpty(message))
            {
                //check if the previous message has any text
                var messages = await Context.Channel.GetMessagesAsync(2).FlattenAsync();
                var priorMessage = messages.Last();
                if (!string.IsNullOrEmpty(priorMessage.Content))
                {
                    wideText = _textEditService.ConvertMessage(priorMessage.Content);
                }
            }
            else 
            {
                wideText = _textEditService.ConvertMessage(message);
            }
            await ReplyAsync(wideText);
        }

        [Command("memecase", RunMode = RunMode.Async)]
        [Summary("Converts the input message (or the contents of the previous message) into meme-case text.")]
        public async Task ConvertMessageToMemeCaseAsync([Summary("the message to convert")][Remainder]string message = null)
        {
            string memeText = string.Empty;
            if (string.IsNullOrEmpty(message))
            {
                //check if the previous message has any text
                var messages = await Context.Channel.GetMessagesAsync(2).FlattenAsync();
                var priorMessage = messages.Last();
                if (!string.IsNullOrEmpty(priorMessage.Content))
                {
                    memeText = _textEditService.ConvertToMemeCase(priorMessage.Content);
                }
            }
            else 
            {
                memeText = _textEditService.ConvertToMemeCase(message);
            }
            await ReplyAsync(memeText);
        }
    }
}
