﻿using System;
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

        public TextEditModule(IConfiguration config, TextEditService service) : base(config)
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
                    wideText = _textEditService.ConvertToFullWidth(priorMessage.Content);
                }
            }
            else 
            {
                wideText = _textEditService.ConvertToFullWidth(message);
            }
            await ReplyAsync(wideText);
        }

        [Command("escape", RunMode = RunMode.Async)]
        [Summary("Escape @mentions, #channels, and other Discord formatting.")]
        public async Task EscapeTextAsync([Summary("the message to convert")][Remainder] string message = null)
        {
            string plaintext = string.Empty;
            if (string.IsNullOrEmpty(message))
            {
                //check if the previous message has any text
                var messages = await Context.Channel.GetMessagesAsync(2).FlattenAsync();
                var priorMessage = messages.Last();
                if (!string.IsNullOrEmpty(priorMessage.Content))
                {
                    plaintext = _textEditService.EscapeText(priorMessage.Content);
                }
            }
            else
            {
                plaintext = _textEditService.EscapeText(message);
            }
            await ReplyAsync(plaintext);
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

        [Command("emojify", RunMode = RunMode.Async)]
        [Summary("Emojifies the current message (or previous if no message passed)")]
        public async Task EmojifyMessageAsync([Summary("the message to convert")][Remainder] string message = null)
        {
            string emojiText = string.Empty;
            if (string.IsNullOrEmpty(message))
            {
                //check if the previous message has any text
                var messages = await Context.Channel.GetMessagesAsync(2).FlattenAsync();
                var priorMessage = messages.Last();
                if (!string.IsNullOrEmpty(priorMessage.Content))
                {
                    emojiText = _textEditService.Emojify(priorMessage.Content);
                }
            }
            else
            {
                emojiText = _textEditService.Emojify(message);
            }
            await ReplyAsync(emojiText);
        }
    }
}
