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

        private async Task<IReadOnlyCollection<Attachment>> GetAttachmentsAsync()
        {
            var attachments = Context.Message.Attachments;
            if (attachments == null || attachments.Count == 0)
            {
                //check if the last message before this one has any attachments
                var priorMessages = await Context.Channel.GetMessagesAsync(2).FlattenAsync();
                if (priorMessages.Last().Attachments.Count > 0)
                {
                    return (IReadOnlyCollection<Attachment>)priorMessages.Last().Attachments;
                }
                else
                {
                    throw new NullReferenceException("No attachments were found in the current or previous message.");
                }
            }
            else
            {
                return attachments;
            }
        }

        private async Task SendImages(List<string> images)
        {
            foreach (var image in images)
            {
                await SendFileAsync(image);
            }

            _imageService.DeleteImages(images);
        }


        [Command("deepfry", RunMode = RunMode.Async)]
        public async Task DeepFryImageAsync(int numPasses = 1)
        {
            IReadOnlyCollection<Attachment> attachments = null;
            try
            {
                attachments = await GetAttachmentsAsync();
            }
            catch (NullReferenceException)
            {
                await ServiceReplyAsync("Please attach an image file.");
            }

            var images = _imageService.DeepfryImages(attachments, numPasses);
            await SendImages(images);
        }

        [Command("morrowind", RunMode = RunMode.Async)]
        public async Task MorrowindImageAsync()
        {
            IReadOnlyCollection<Attachment> attachments = null;
            try
            {
                attachments = await GetAttachmentsAsync();
            }
            catch (NullReferenceException)
            {
                await ServiceReplyAsync("Please attach an image file.");
            }

            var images = _imageService.MorrowindImages(attachments);
            await SendImages(images);
        }

        [Command("gimp", RunMode = RunMode.Async)]
        public async Task MosaicImageAsync()
        {
            IReadOnlyCollection<Attachment> attachments = null;
            try
            {
                attachments = await GetAttachmentsAsync();
            }
            catch (NullReferenceException)
            {
                await ServiceReplyAsync("Please attach an image file.");
            }

            var images = _imageService.MosaicImages(attachments);
            await SendImages(images);
        }

        [Command("meme", RunMode = RunMode.Async)]
        public async Task MemeCaptionImageAsync(string topText = null, string bottomText = null)
        {
            IReadOnlyCollection<Attachment> attachments = null;
            try
            {
                attachments = await GetAttachmentsAsync();
            }
            catch (NullReferenceException)
            {
                await ServiceReplyAsync("Please attach an image file.");
            }

            if (topText == null || bottomText == null)
            {
                await ServiceReplyAsync("Please add a caption.");
                return;
            }

            var images = _imageService.MemeCaptionImages(attachments, topText, bottomText);
            await SendImages(images);
        }
    }
}
