using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using TerminusDotNetCore.Services;
using TerminusDotNetCore.Helpers;
using Microsoft.Extensions.Configuration;
using Discord.Interactions;

namespace TerminusDotNetCore.Modules
{
    public enum ParamType
    {
        Numeric,
        Text,
        None
    }

    public class ImageModule : InteractionModule
    {
        private ImageService _imageService;

        private const string NO_ATTACHMENTS_FOUND_MESSAGE = "No images found in the current or previous messages.";

        public ImageModule(IConfiguration config, ImageService service) : base(config)
        {
            _imageService = service;
            _imageService.Config = config;
            _imageService.ParentModule = this;
        }

        private async Task SendImages(List<string> images)
        {
            try
            {
                List<FileAttachment> attachments = new List<FileAttachment>();
                foreach (string image in images)
                {
                    attachments.Add(new FileAttachment(image));
                }
                await Context.Interaction.RespondWithFilesAsync(attachments);
            }
            finally
            {
                _imageService.DeleteImages(images);
            }
        }

        private async Task SendImage(string image)
        {
            await Context.Interaction.RespondWithFileAsync(image);
            System.IO.File.Delete(image);
        }

        [SlashCommand("mirror", "Mirror the given image across an axis")]
        public async Task MirrorImagesAsync([Summary(description: "axis to mirror the image on (`horizontal` or `vertical`)")]string flipMode = "horizontal")
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.MirrorImages(attachments, flipMode);
            await SendImages(images);
        }

        [SlashCommand("deepfry", "Deepfry the given image")]
        public async Task DeepFryImageAsync([Summary(description: "how much to fry the image")]uint deepfryFactor = 1)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.DeepfryImages(attachments, deepfryFactor);
            await SendImages(images);
        }

        [SlashCommand("grayscale", "Convert the given image to grayscale")]
        public async Task GrayscaleImageAsync()
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.GrayscaleImages(attachments);
            await SendImages(images);
        }

        //[SlashCommand("polaroid", "Apply a shitty Polaroid filter to the given image")]
        public async Task PolaroidImageAsync()
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.PolaroidImages(attachments);
            await SendImages(images);
        }

        //[SlashCommand("kodak", "Apply a shitty Kodachrome filter to the given image")]
        public async Task KodakImageAsync()
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.KodakImages(attachments);
            await SendImages(images);
        }

        //[SlashCommand("invert", "Invert the colors of the given image")]
        public async Task InvertImageAsync()
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.InvertImages(attachments);
            await SendImages(images);
        }

        //[SlashCommand("initiald", "NANI???? KANSEI DORIFTO?!?!?!?!?")]
        public async Task InitialDImagesAsync()
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.InitialDImages(attachments);
            await SendImages(images);
        }

        //[SlashCommand("reddit", "Apply le funny REDDIT watermark to the given image")]
        public async Task RedditWatermarkImagesAsync([Summary(description: "subreddit name")] string subName = "")
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.RedditWatermarkImages(attachments, subName);
            await SendImages(images);
        }

        //[SlashCommand("morrowind", "With this character's death, the thread of prophecy is severed.")]
        public async Task MorrowindImageAsync()
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.MorrowindImages(attachments);
            await SendImages(images);
        }

        //[SlashCommand("dmc", "Featuring Dante from the Devil May Cry series")]
        public async Task DMCWatermarkImagesAsync()
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.DMCWatermarkImages(attachments);
            await SendImages(images);
        }

        //[SlashCommand("bebop", "SEE YOU SPACE COWBOY...")]
        public async Task BebopWatermarkImagesAsync()
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.BebopWatermarkImages(attachments);
            await SendImages(images);
        }

        //[SlashCommand("nintendo", "Add a Nintendo seal of approval to the given image")]
        public async Task NintendoWatermarkImagesAsync()
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.NintendoWatermarkImages(attachments);
            await SendImages(images);
        }

        //[SlashCommand("gimp", "Funny GNU pepper command haha")]
        public async Task MosaicImageAsync()
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.MosaicImages(attachments);
            await SendImages(images);
        }

        //[SlashCommand("meme", "BOTTOM TEXT")]
        public async Task MemeCaptionImageAsync([Summary(description: "top text to add")]string topText = null, [Summary(description: "bottom text to add")]string bottomText = null)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            if (topText == null && bottomText == null)
            {
                await RespondAsync("Please add a caption.");
                return;
            }

            var images = _imageService.MemeCaptionImages(attachments, topText, bottomText);
            await SendImages(images);
        }

        //[SlashCommand("thicc", "Stretch the given image")]
        public async Task ThiccImageAsync([Summary(description: "factor to scale the image width by")]int thiccFactor = 2)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.ThiccImages(attachments, thiccFactor);
            await SendImages(images);
        }

        //[SlashCommand("pixelate", "Pixelate the given image")]
        public async Task PixelateImageAsync([Summary(description: "Pixel size")]int pixelSize = 0)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.PixelateImages(attachments, pixelSize);
            await SendImages(images);
        }

        //[SlashCommand("contrast", "Change the contrast of the given image")]
        public async Task ContrastImageAsync([Summary(description:"Contrast amount")]float amount = 2.0f)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.ContrastImages(attachments, amount);
            await SendImages(images);
        }

        //[SlashCommand("saturate", "Change the saturation of the given image")]
        public async Task SaturateImageAsync([Summary("Contrast amount")]float amount = 2.0f)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            var images = _imageService.SaturateImages(attachments, amount);
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

        //[SlashCommand("bobross", "Draw the given image on Bob's canvas")]
        public async Task BobRossImagesAsync([Summary(description: "Text to project onto the canvas. Numeric value repeats the projection.")]string text = null)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null && text == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            ParamType paramType = ParseParamType(text);
            List<string> images = new List<string>();

            switch (paramType)
            {
                case ParamType.Numeric:
                    images = _imageService.ProjectImagesOnto("bobross.json", attachments, uint.Parse(text));
                    await SendImages(images);
                    break;

                case ParamType.Text:
                    string textImg = _imageService.ProjectTextOnto("bobross.json", text);
                    await SendImage(textImg);
                    break;

                case ParamType.None:
                    images = _imageService.ProjectImagesOnto("bobross.json", attachments);
                    await SendImages(images);
                    break;
            }
        }

        //[SlashCommand("pc", "I want to KMS")]
        public async Task PCImagesAsync([Summary(description: "Text to project onto the canvas. Numeric value repeats the projection.")]string text = null)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null && text == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            ParamType paramType = ParseParamType(text);
            List<string> images = new List<string>();

            switch (paramType)
            {
                case ParamType.Numeric:
                    images = _imageService.ProjectImagesOnto("pc.json", attachments, uint.Parse(text));
                    await SendImages(images);
                    break;

                case ParamType.Text:
                    string textImg = _imageService.ProjectTextOnto("pc.json", text);
                    await SendImage(textImg);
                    break;

                case ParamType.None:
                    images = _imageService.ProjectImagesOnto("pc.json", attachments);
                    await SendImages(images);
                    break;
            }
        }

        //[SlashCommand("trump", "Orang man")]
        public async Task TrumpImagesAsync([Summary(description: "Text to project onto the canvas. Numeric value repeats the projection.")]string text = null)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null && text == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            ParamType paramType = ParseParamType(text);
            List<string> images = new List<string>();

            switch (paramType)
            {
                case ParamType.Numeric:
                    images = _imageService.ProjectImagesOnto("trump.json", attachments, uint.Parse(text));
                    await SendImages(images);
                    break;

                case ParamType.Text:
                    string textImg = _imageService.ProjectTextOnto("trump.json", text);
                    await SendImage(textImg);
                    break;

                case ParamType.None:
                    images = _imageService.ProjectImagesOnto("trump.json", attachments);
                    await SendImages(images);
                    break;
            }
        }

        //[SlashCommand("walter", "Doctor D")]
        public async Task WalterImagesAsync([Summary(description: "Text to project onto the canvas. Numeric value repeats the projection.")]string text = null)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null && text == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            ParamType paramType = ParseParamType(text);
            List<string> images = new List<string>();

            switch (paramType)
            {
                case ParamType.Numeric:
                    images = _imageService.ProjectImagesOnto("walter.json", attachments, uint.Parse(text));
                    await SendImages(images);
                    break;

                case ParamType.Text:
                    string textImg = _imageService.ProjectTextOnto("walter.json", text);
                    await SendImage(textImg);
                    break;

                case ParamType.None:
                    images = _imageService.ProjectImagesOnto("walter.json", attachments);
                    await SendImages(images);
                    break;
            }
        }

        //[SlashCommand("hank", "Hink Hall")]
        public async Task HankImagesAsync([Summary(description: "Text to project onto the canvas. Numeric value repeats the projection.")]string text = null)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null && text == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            ParamType paramType = ParseParamType(text);
            List<string> images = new List<string>();

            switch (paramType)
            {
                case ParamType.Numeric:
                    images = _imageService.ProjectImagesOnto("hank.json", attachments, uint.Parse(text));
                    await SendImages(images);
                    break;

                case ParamType.Text:
                    string textImg = _imageService.ProjectTextOnto("hank.json", text);
                    await SendImage(textImg);
                    break;

                case ParamType.None:
                    images = _imageService.ProjectImagesOnto("hank.json", attachments);
                    await SendImages(images);
                    break;
            }
        }

        //[SlashCommand("emmy", "Emmy")]
        public async Task EmmyImagesAsync([Summary(description: "Text to project onto the canvas. Numeric value repeats the projection.")] string text = null)
        {
            IReadOnlyCollection<Attachment> attachments = await AttachmentHelper.GetMostRecentAttachmentsAsync(Context, AttachmentFilter.Images);
            if (attachments == null && text == null)
            {
                await RespondAsync(NO_ATTACHMENTS_FOUND_MESSAGE);
                return;
            }

            ParamType paramType = ParseParamType(text);
            List<string> images = new List<string>();

            switch (paramType)
            {
                case ParamType.Numeric:
                    images = _imageService.ProjectImagesOnto("emmy.json", attachments, uint.Parse(text));
                    await SendImages(images);
                    break;

                case ParamType.Text:
                    string textImg = _imageService.ProjectTextOnto("emmy.json", text);
                    await SendImage(textImg);
                    break;

                case ParamType.None:
                    images = _imageService.ProjectImagesOnto("emmy.json", attachments);
                    await SendImages(images);
                    break;
            }
        }
    }
}
