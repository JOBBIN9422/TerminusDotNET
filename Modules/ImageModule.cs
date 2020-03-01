using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using TerminusDotNetCore.Services;
using TerminusDotNetCore.Helpers;

namespace TerminusDotNetCore.Modules
{
    public enum ParamType
    {
        Numeric,
        Text,
        None
    }

    public class ImageModule : ServiceControlModule
    {
        private ImageService _imageService;

        public ImageModule(ImageService service)
        {
            _imageService = service;
            _imageService.ParentModule = this;
        }

        public async Task SendFileAsync(string filename)
        {
            await Context.Channel.SendFileAsync(filename);
        }

        private async Task SendImages(List<string> images)
        {
            try
            {
                foreach (var image in images)
                {
                    try
                    {
                        await SendFileAsync(image);
                    }
                    catch (Exception)
                    {
                        await ServiceReplyAsync($"Error sending file {System.IO.Path.GetFileName(image)}.");
                    }
                }
            }
            finally
            {
                _imageService.DeleteImages(images);
            }
        }

        private async Task SendImage(string image)
        {
            await SendFileAsync(image);
            System.IO.File.Delete(image);
        }

        [Command("deepfry", RunMode = RunMode.Async)]
        [Summary("Deep-fries an attached image, or the image in the previous message (if any).")]
        public async Task DeepFryImageAsync([Summary("how many times to fry the image")]uint numPasses = 1)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await ServiceReplyAsync("No images were found in the current message or previous messages.");
                return;
            }

            var images = _imageService.DeepfryImages(attachments, numPasses);
            await SendImages(images);
        }

        [Command("morrowind", RunMode = RunMode.Async)]
        [Summary("Places a Morrowind prompt on the attached image, or the image in the previous message (if any).")]
        public async Task MorrowindImageAsync()
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await ServiceReplyAsync("No images were found in the current message or previous messages.");
                return;
            }

            var images = _imageService.MorrowindImages(attachments);
            await SendImages(images);
        }

        [Command("dmc", RunMode = RunMode.Async)]
        [Summary("Places a DMC watermark on the attached image, or the image in the previous message (if any).")]
        public async Task DMCWatermarkImagesAsync()
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await ServiceReplyAsync("No images were found in the current message or previous messages.");
                return;
            }

            var images = _imageService.DMCWatermarkImages(attachments);
            await SendImages(images);
        }

        [Command("gimp", RunMode = RunMode.Async)]
        [Summary("Converts the attached image (or the image in the previous message) into a GIMP pepper mosaic.")]

        public async Task MosaicImageAsync()
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await ServiceReplyAsync("No images were found in the current message or previous messages.");
                return;
            }

            var images = _imageService.MosaicImages(attachments);
            await SendImages(images);
        }

        [Command("meme", RunMode = RunMode.Async)]
        [Summary("Adds top text and bottom text to the attached image, or the image in the previous message (if any).")]
        public async Task MemeCaptionImageAsync([Summary("top text to add")]string topText = null, [Summary("bottom text to add")]string bottomText = null)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await ServiceReplyAsync("No images were found in the current message or previous messages.");
                return;
            }

            if (topText == null && bottomText == null)
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
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await ServiceReplyAsync("No images were found in the current message or previous messages.");
                return;
            }

            var images = _imageService.ThiccImages(attachments, thiccCount);
            await SendImages(images);
        }

        private ParamType ParseParamType(string paramText)
        {
            if (!string.IsNullOrEmpty(paramText))
            {
                uint outVal;
                if (uint.TryParse(paramText, out outVal))
                {
                    return ParamType.Numeric;
                }
                else
                {
                    return ParamType.Text;
                }
            }
            else
            {
                return ParamType.None;
            }
        }

        [Command("bobross", RunMode = RunMode.Async)]
        [Summary("Paints the attached/most recent image(s) on Bob's happy little canvas. If text is supplied, draws the text on Bob's canvas instead.")]
        public async Task BobRossImagesAsync([Remainder][Summary("Text to project onto the canvas. If a number is supplied, number of times to repeat the projection instead.")]string text = null)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await ServiceReplyAsync("No images were found in the current message or previous messages.");
                return;
            }

            ParamType paramType = ParseParamType(text);
            List<string> images = new List<string>();

            switch (paramType)
            {
                case ParamType.Numeric:
                    images = _imageService.BobRossImages(attachments, uint.Parse(text));
                    await SendImages(images);
                    break;

                case ParamType.Text:
                    string bobRossTextImg = _imageService.BobRossText(text);
                    await SendImage(bobRossTextImg);
                    break;

                case ParamType.None:
                    images = _imageService.BobRossImages(attachments);
                    await SendImages(images);
                    break;
            }
        }

        [Command("pc", RunMode = RunMode.Async)]
        [Summary("Paints the attached/most recent image(s) on a stock photo of someone at their computer.")]
        public async Task PCImagesAsync([Remainder][Summary("Text to project onto the canvas. If a number is supplied, number of times to repeat the projection instead.")]string text = null)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await ServiceReplyAsync("No images were found in the current message or previous messages.");
                return;
            }

            ParamType paramType = ParseParamType(text);
            List<string> images = new List<string>();

            switch (paramType)
            {
                case ParamType.Numeric:
                    images = _imageService.PCImages(attachments, uint.Parse(text));
                    await SendImages(images);
                    break;

                case ParamType.Text:
                    string bobRossTextImg = _imageService.PCText(text);
                    await SendImage(bobRossTextImg);
                    break;

                case ParamType.None:
                    images = _imageService.PCImages(attachments);
                    await SendImages(images);
                    break;
            }
        }

        [Command("trump", RunMode = RunMode.Async)]
        [Summary("Overlays a custom image attachment onto a book held by President Trump.")]
        public async Task TrumpImagesAsync([Remainder][Summary("Text to project onto the canvas. If a number is supplied, number of times to repeat the projection instead.")]string text = null)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await ServiceReplyAsync("No images were found in the current message or previous messages.");
                return;
            }

            ParamType paramType = ParseParamType(text);
            List<string> images = new List<string>();

            switch (paramType)
            {
                case ParamType.Numeric:
                    images = _imageService.TrumpImages(attachments, uint.Parse(text));
                    await SendImages(images);
                    break;

                case ParamType.Text:
                    string bobRossTextImg = _imageService.TrumpText(text);
                    await SendImage(bobRossTextImg);
                    break;

                case ParamType.None:
                    images = _imageService.TrumpImages(attachments);
                    await SendImages(images);
                    break;
            }
        }

        [Command("walter", RunMode = RunMode.Async)]
        [Summary("Overlays a custom image attachment onto Dr. D's whiteboard.")]
        public async Task WalterImagesAsync([Remainder][Summary("Text to project onto the canvas. If a number is supplied, number of times to repeat the projection instead.")]string text = null)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);

            //check if an argument was provided
            if (!string.IsNullOrEmpty(text))
            {
                //is the argument solely a number?
                uint numTimes;
                if (uint.TryParse(text, out numTimes) && attachments != null)
                {
                    var images = _imageService.WalterImages(attachments, numTimes);
                    await SendImages(images);
                }

                //if not, treat it as text
                else
                {
                    string bobRossTextImg = _imageService.WalterText(text);
                    await SendImage(bobRossTextImg);
                }
            }

            //if no argument was provided, try to process each image once
            else
            {
                if (attachments == null)
                {
                    await ServiceReplyAsync("No images were found in the current message or previous messages.");
                    return;
                }

                var images = _imageService.WalterImages(attachments);
                await SendImages(images);
            }
        }
    }
}
