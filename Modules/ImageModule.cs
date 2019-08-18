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

        public async Task ServiceReplyAsync(string s, EmbedBuilder embedBuilder = null)
        {
            if (embedBuilder == null)
            {
                await ReplyAsync(s);
            }
            else
            {
                await ReplyAsync(s, false, embedBuilder.Build());
            }
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
        public async Task DeepFryImageAsync(int numPasses = 1)
        {
            var attachments = Context.Message.Attachments;
            if (attachments == null || attachments.Count == 0)
            {
                await ServiceReplyAsync("Please attach an image file.");
                return;
            }

            var images = _imageService.DeepfryImages(attachments, numPasses);

            foreach (var image in images)
            {
                await SendFileAsync(image);
            }

            _imageService.DeleteImages(images);
        }

        [Command("morrowind", RunMode = RunMode.Async)]
        public async Task MorrowindImageAsync()
        {
            var attachments = Context.Message.Attachments;
            if (attachments == null || attachments.Count == 0)
            {
                await ServiceReplyAsync("Please attach an image file.");
                return;
            }

            var images = _imageService.MorrowindImages(attachments);

            foreach (var image in images)
            {
                await SendFileAsync(image);
            }

            _imageService.DeleteImages(images);
        }

        [Command("meme", RunMode = RunMode.Async)]
        public async Task MemeCaptionImageAsync(string topText = null, string bottomText = null)
        {
            var attachments = Context.Message.Attachments;
            if (attachments == null || attachments.Count == 0)
            {
                await ServiceReplyAsync("Please attach an image file.");
                return;
            }

            if (topText == null || bottomText == null)
            {
                await ServiceReplyAsync("Please add a caption.");
                return;
            }

            var images = _imageService.MemeCaptionImages(attachments, topText, bottomText);

            foreach (var image in images)
            {
                await SendFileAsync(image);
            }

            _imageService.DeleteImages(images);
        }
    }
}
