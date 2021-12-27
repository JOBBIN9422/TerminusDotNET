﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using TerminusDotNetCore.Services;

namespace TerminusDotNetCore.Modules
{
    public class TextEditModule : InteractionModule
    {
        private TextEditService _textEditService;

        public TextEditModule(IConfiguration config, TextEditService service) : base(config)
        {
            _textEditService = service;
            _textEditService.Config = config;
            _textEditService.ParentModule = this;
        }

        [SlashCommand("echo", "Echo the input")]
        public async Task Echo(string input)
        {
            await RespondAsync(input);
        }

        [SlashCommand("wide", "Convert the input to full-width text")]
        public async Task ConvertMessageToFullWidthAsync(string input)
        {
            await DeferAsync();
            string output = _textEditService.ConvertToFullWidth(input);
            await RespondAsync(output);
        }

        [SlashCommand("escape", "Display the input as plaintext (escape @mentions, #channels, and other Discord formatting)")]
        public async Task EscapeTextAsync(string input)
        {
            await DeferAsync();
            string output = _textEditService.EscapeText(input);
            await RespondAsync(output);
        }

        [SlashCommand("memecase", "Convert the input to MeMeCaSe")]
        public async Task ConvertMessageToMemeCaseAsync(string input)
        {
            await DeferAsync();
            string output = _textEditService.ConvertToMemeCase(input);
            await RespondAsync(output);
        }

        [SlashCommand("emojify", "Emojify the input")]
        public async Task EmojifyMessageAsync(string input)
        {
            await DeferAsync();
            string output = _textEditService.Emojify(input);
            await RespondAsync(output);
        }
    }
}
