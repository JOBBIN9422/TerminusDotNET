using System;
using System.Collections.Generic;
using System.IO;
using Discord;
using TerminusDotNetCore.Modules;
using System.Drawing;
using TerminusDotNetCore.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using System.Numerics;
using SixLabors.Fonts;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp.Drawing.Processing;
using SolidBrush = SixLabors.ImageSharp.Drawing.Processing.SolidBrush;

namespace TerminusDotNetCore.Services
{
    public class ImageService : IInteractionService
    {
        public InteractionModule ParentModule { get; set; }
        public IConfiguration Config { get; set; }

        public List<string> DeepfryImages(IReadOnlyCollection<Attachment> attachments, uint numPasses = 1)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var deepFriedImg = ImageHelper.DeepfryImage(image, numPasses))
                {
                    deepFriedImg.Save(image);
                }
            }

            return images;
        }

        public List<string> GrayscaleImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var grayscaleImg = ImageHelper.GrayscaleImage(image))
                {
                    grayscaleImg.Save(image);
                }
            }

            return images;
        }

        public List<string> PolaroidImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var polaroidImg = ImageHelper.PolaroidImage(image))
                {
                    polaroidImg.Save(image);
                }
            }

            return images;
        }

        public List<string> InvertImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var invertedImg = ImageHelper.InvertImage(image))
                {
                    invertedImg.Save(image);
                }
            }

            return images;
        }

        public List<string> KodakImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var kodakImg = ImageHelper.KodakImage(image))
                {
                    kodakImg.Save(image);
                }
            }

            return images;
        }

        public List<string> PixelateImages(IReadOnlyCollection<Attachment> attachments, int size)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var pixelatedImg = ImageHelper.PixelateImage(image, size))
                {
                    pixelatedImg.Save(image);
                }
            }

            return images;
        }

        public List<string> ContrastImages(IReadOnlyCollection<Attachment> attachments, float amount)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var contrastImg = ImageHelper.ContrastImage(image, amount))
                {
                    contrastImg.Save(image);
                }
            }

            return images;
        }

        public List<string> SaturateImages(IReadOnlyCollection<Attachment> attachments, float amount)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var saturatedImg = ImageHelper.SaturateImage(image, amount))
                {
                    saturatedImg.Save(image);
                }
            }

            return images;
        }

        public void DeleteImages(List<string> images)
        {
            AttachmentHelper.DeleteFiles(images);
        }

        public List<string> InitialDImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var initialDImg = SixLabors.ImageSharp.Image.Load(Path.Combine("assets", "images", "initial-d.png")))
                using (var baseImg = new Image<Rgba32>(initialDImg.Width, initialDImg.Height))
                using (var userImg = SixLabors.ImageSharp.Image.Load(image))
                {
                    //scale the input image to 65% of initial d img height to get the full pic in the winshield (dude trust me)
                    int newHeight = (int)(initialDImg.Height * 0.65);
                    userImg.Mutate(x => x.Resize(initialDImg.Width, newHeight));

                    //draw scaled-down input and initial d overlay on the blank image 
                    baseImg.Mutate(x => x.DrawImage(userImg, 1.0f));
                    baseImg.Mutate(x => x.DrawImage(initialDImg, 1.0f));

                    baseImg.Save(image);
                }
            }

            return images;
        }

        public List<string> RedditWatermarkImages(IReadOnlyCollection<Attachment> attachments, string subName)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);
            SixLabors.Fonts.Font robotoFont = SixLabors.Fonts.SystemFonts.CreateFont("Roboto", 60.0f);

            foreach (var image in images)
            {
                using (var redditImg = SixLabors.ImageSharp.Image.Load(Path.Combine("assets", "images", "reddit.png")))
                using (var userImg = SixLabors.ImageSharp.Image.Load(image))
                using (var baseImg = new Image<Rgba32>(userImg.Width, userImg.Height))
                {
                    //draw subreddit name on watermark - @ (377, 79)?
                    redditImg.Mutate(x => x.DrawText(subName, robotoFont, SixLabors.ImageSharp.Color.White, new SixLabors.ImageSharp.PointF(375, 56)));

                    //scale watermark to img dimensions (maintain aspect ratio)
                    redditImg.Mutate(x => x.Resize(baseImg.Width, 0));

                    baseImg.Mutate(x => x.Resize(baseImg.Width, baseImg.Height + redditImg.Height));

                    //draw input img and watermark on base image
                    baseImg.Mutate(x => x.DrawImage(userImg, new SixLabors.ImageSharp.Point(0, 0), 1.0f));
                    baseImg.Mutate(x => x.DrawImage(redditImg, new SixLabors.ImageSharp.Point(0, baseImg.Height - redditImg.Height), 1.0f));

                    //userImg.Mutate(x => x.DrawImage(redditImg, new SixLabors.ImageSharp.Point(0, userImg.Height - redditImg.Height), 1.0f));

                    baseImg.Save(image);
                }
            }

            return images;
        }

        public List<string> MorrowindImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var morrowindImg = ImageHelper.WatermarkImage(image, Path.Combine("assets", "images", "morrowind.png"), AnchorPositionMode.Bottom, 0.1, 0.1, 0.67))
                {
                    morrowindImg.Save(image);
                }
            }

            return images;
        }

        public List<string> DMCWatermarkImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var dmcImg = ImageHelper.WatermarkImage(image, Path.Combine("assets", "images", "dmc.png"), AnchorPositionMode.BottomLeft, 0.1, 0.1, 0.25))
                {
                    dmcImg.Save(image);
                }
            }

            return images;
        }

        public List<string> BebopWatermarkImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var bebopImg = ImageHelper.WatermarkImage(image, Path.Combine("assets", "images", "bebop.png"), AnchorPositionMode.BottomRight, 0.05, 0.05, 0.6, 1.0f))
                {
                    bebopImg.Save(image);
                }
            }

            return images;
        }

        public List<string> NintendoWatermarkImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var nintendoImg = ImageHelper.WatermarkImage(image, Path.Combine("assets", "images", "nintendo.png"), AnchorPositionMode.BottomRight, 0.1, 0.1, 0.25))
                {
                    nintendoImg.Save(image);
                }
            }

            return images;
        }

        public List<string> MemeCaptionImages(IReadOnlyCollection<Attachment> attachments, string topText, string bottomText)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var bottomTextLmao = ImageHelper.MemeCaptionImage(image, topText, bottomText))
                {
                    bottomTextLmao.Save(image);
                }
            }

            return images;
        }

        public List<string> ThiccImages(IReadOnlyCollection<Attachment> attachments, int thiccCount = 2)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var thiccImg = ImageHelper.ThiccImage(image, thiccCount))
                {
                    thiccImg.Save(image);
                }
            }

            return images;
        }

        public List<string> MosaicImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                using (var mosaicImg = ImageHelper.MosaicImage(image, Path.Combine("assets", "images", "GIMP_Pepper.png"), 0.02, 0.5f))
                {
                    mosaicImg.Save(image);
                }
            }

            return images;
        }

        public string MirrorImage(IAttachment image, string flipModeStr, string flipSideStr)
        {
            FlipMode flipMode = FlipMode.Horizontal;
            if (flipModeStr == "vertical" || flipModeStr == "vert")
            {
                flipMode = FlipMode.Vertical;
            }
            MirrorImage(image.Filename, flipMode, flipSideStr.ToLower() == "heads");
        }

        public List<string> MirrorImages(IReadOnlyCollection<Attachment> attachments, string flipModeStr)
        {
            var returnImgs = new List<string>();
            var imagesFirstHalf = AttachmentHelper.DownloadAttachments(attachments);
            returnImgs.AddRange(imagesFirstHalf);

            FlipMode flipMode = FlipMode.Horizontal;
            if (flipModeStr == "vertical" || flipModeStr == "vert")
            {
                flipMode = FlipMode.Vertical;
            }

            //mirror the images one way
            foreach (var image in imagesFirstHalf)
            {
                MirrorImage(image, flipMode);
            }

            var imagesSecondHalf = AttachmentHelper.DownloadAttachments(attachments);
            returnImgs.AddRange(imagesSecondHalf);

            //and mirror the images the other way
            foreach(var image in imagesSecondHalf)
            {
                MirrorImage(image, flipMode, false);
            }

            return returnImgs;
        }

        private void MirrorImage(string imageFilename, FlipMode flipMode, bool topAndOrLeftHalf = true)
        {
            using (var image = ImageHelper.MirrorImage(imageFilename, flipMode, topAndOrLeftHalf))
            {
                image.Save(imageFilename);
            }
        }

        public List<string> ProjectImagesOnto(string projectionFilename, IReadOnlyCollection<Attachment> attachments, uint numTimes = 1)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                for (uint i = 0; i < numTimes; i++)
                {
                    using (var projectionImg = ImageHelper.ProjectOnto(image, Path.Combine("assets", "images", projectionFilename)))
                    {
                        projectionImg.Save(image);
                    }
                }
            }

            return images;
        }

        public string ProjectTextOnto(string projectionFilename, string text)
        {
            {
                using (var image = ImageHelper.ProjectText(text, Path.Combine("assets", "images", projectionFilename)))
                {
                    string outputFilename = $"{Guid.NewGuid().ToString("N")}.jpg";
                    image.Save(outputFilename);

                    return outputFilename;
                }
            }
        }
    }
}
