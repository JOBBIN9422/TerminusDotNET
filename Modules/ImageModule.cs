using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using TerminusDotNetConsoleApp.Services;

namespace TerminusDotNetConsoleApp.Modules
{
    public class ImageModule : ModuleBase<SocketCommandContext>, IServiceModule
    {
        private ImageService _imageService;

        public ImageModule(ImageService service)
        {
            _imageService = service;
            _imageService.ParentModule = this;
        }

        public async Task ServiceReplyAsync(string s)
        {
            await ReplyAsync(s);
        }

        public async Task ServiceReplyAsync(string title, EmbedBuilder embedBuilder)
        {
            await ReplyAsync(title, false, embedBuilder.Build());
        }

        public async Task SendFileAsync(string filename)
        {
            var embed = new EmbedBuilder()
            {
                ImageUrl = $"attachment://{filename}"
            }.Build();

            await Context.Channel.SendFileAsync(filename, embed: embed);
        }

        [Command("deepfry", RunMode = RunMode.Async)]
        public async Task DeepFryImageAsync()
        {
            var attachments = Context.Message.Attachments;
            if (attachments == null || attachments.Count == 0)
            {
                await ServiceReplyAsync("Please attach an image file.");
                return;
            }

            var images = _imageService.DeepfryImages(attachments);

            foreach (var image in images)
            {
                await SendFileAsync(image);
            }

            _imageService.DeleteImages(images);
        }

    }
}
