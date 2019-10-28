using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using TerminusDotNetCore.Services;

namespace TerminusDotNetCore.Modules
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
            //var embed = new EmbedBuilder()
            //{
            //    ImageUrl = $"attachment://{filename}"
            //}.Build();

            //await Context.Channel.SendFileAsync(filename, embed: embed);
            await Context.Channel.SendFileAsync(filename);
        }

        private async Task<IReadOnlyCollection<Attachment>> GetAttachmentsAsync()
        {
            var attachments = Context.Message.Attachments;
            if (attachments == null || attachments.Count == 0)
            {
                //check if the last message before this one has any attachments
                var messages = await Context.Channel.GetMessagesAsync(2).FlattenAsync();
                var priorMessage = messages.Last();
                if (priorMessage.Attachments.Count > 0)
                {
                    return (IReadOnlyCollection<Attachment>)priorMessage.Attachments;
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
        [Summary("Deep-fries an attached image, or the image in the previous message (if any).")]
        public async Task DeepFryImageAsync([Summary("how many times to fry the image")]int numPasses = 1)
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
        [Summary("Places a Morrowind prompt on the attached image, or the image in the previous message (if any).")]
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
        [Summary("Converts the attached image (or the image in the previous message) into a GIMP pepper mosaic.")]

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
        [Summary("Adds top text and bottom text to the attached image, or the image in the previous message (if any).")]
        public async Task MemeCaptionImageAsync([Summary("top text to add")]string topText = null, [Summary("bottom text to add")]string bottomText = null)
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

        [Command("thicc", RunMode = RunMode.Async)]
        [Summary("Stretches the attached image, or the image in the previous message (if any).")]

        public async Task ThiccImageAsync([Summary("factor to scale the image width by")]int thiccCount = 2)
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

            var images = _imageService.ThiccImages(attachments, thiccCount);
            await SendImages(images);
        }
    }
}
