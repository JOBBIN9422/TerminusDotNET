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

namespace TerminusDotNetCore.Services
{
    public class ImageService : ICustomService
    {
        public ServiceControlModule ParentModule { get; set; }
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

        public List<string> BobRossImages(IReadOnlyCollection<Attachment> attachments, uint numTimes = 1)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                for (uint i = 0; i < numTimes; i++)
                {
                    using (var bobRossImg = ImageHelper.ProjectOnto(image, Path.Combine("assets", "images", "bobross.json")))
                    {
                        bobRossImg.Save(image);
                    }
                }
            }

            return images;
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

        public string BobRossText(string text)
        {
            using (var image = ImageHelper.ProjectText(text, Path.Combine("assets", "images", "bobross.json")))
            {
                string outputFilename = $"{Guid.NewGuid().ToString("N")}.jpg";
                image.Save(outputFilename);

                return outputFilename;
            }
        }

        public List<string> PCImages(IReadOnlyCollection<Attachment> attachments, uint numTimes = 1)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                for (uint i = 0; i < numTimes; i++)
                {
                    using (var pcImg = ImageHelper.ProjectOnto(image, Path.Combine("assets", "images", "pc.json")))
                    {
                        pcImg.Save(image);
                    }
                }
            }

            return images;
        }

        public string PCText(string text)
        {
            using (var image = ImageHelper.ProjectText(text, Path.Combine("assets", "images", "pc.json")))
            {
                string outputFilename = $"{Guid.NewGuid().ToString("N")}.jpg";
                image.Save(outputFilename);

                return outputFilename;
            }
        }

        public List<string> WalterImages(IReadOnlyCollection<Attachment> attachments, uint numTimes = 1)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                for (uint i = 0; i < numTimes; i++)
                {
                    using (var walterImg = ImageHelper.ProjectOnto(image, Path.Combine("assets", "images", "walter.json")))
                    {
                        walterImg.Save(image);
                    }
                }
            }

            return images;
        }

        public List<string> TrumpImages(IReadOnlyCollection<Attachment> attachments, uint numTimes = 1)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                for (uint i = 0; i < numTimes; i++)
                {
                    using (var trumpImg = ImageHelper.ProjectOnto(image, Path.Combine("assets", "images", "trump.json")))
                    {
                        trumpImg.Save(image);
                    }
                }
            }

            return images;
        }

        public List<string> HankImages(IReadOnlyCollection<Attachment> attachments, uint numTimes = 1)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                for (uint i = 0; i < numTimes; i++)
                {
                    using (var hankImg = ImageHelper.ProjectOnto(image, Path.Combine("assets", "images", "hank.json")))
                    {
                        hankImg.Save(image);
                    }
                }
            }

            return images;
        }

        public List<string> EmmyImages(IReadOnlyCollection<Attachment> attachments, uint numTimes = 1)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                EmmyImage(image, numTimes);
            }

            return images;
        }

        private void TrumpImage(string imageFilename, uint numTimes = 1)
        {
            for (uint i = 0; i < numTimes; i++)
            {
                using (var image = ImageHelper.ProjectOnto(imageFilename, Path.Combine("assets", "images", "trump.json")))
                {
                    image.Save(imageFilename);
                }
            }
        }

        public string TrumpText(string text)
        {
            using (var image = ImageHelper.ProjectText(text, Path.Combine("assets", "images", "trump.json")))
            {
                string outputFilename = $"{Guid.NewGuid().ToString("N")}.jpg";
                image.Save(outputFilename);

                return outputFilename;
            }
        }

        private void WalterImage(string imageFilename, uint numTimes = 1)
        {
            for (uint i = 0; i < numTimes; i++)
            {
                using (var image = ImageHelper.ProjectOnto(imageFilename, Path.Combine("assets", "images", "walter.json")))
                {
                    image.Save(imageFilename);
                }
            }
        }

        public string WalterText(string text)
        {
            using (var image = ImageHelper.ProjectText(text, Path.Combine("assets", "images", "walter.json")))
            {
                string outputFilename = $"{Guid.NewGuid().ToString("N")}.jpg";
                image.Save(outputFilename);

                return outputFilename;
            }
        }

        private void HankImage(string imageFilename, uint numTimes = 1)
        {
            for (uint i = 0; i < numTimes; i++)
            {
                using (var image = ImageHelper.ProjectOnto(imageFilename, Path.Combine("assets", "images", "hank.json")))
                {
                    image.Save(imageFilename);
                }
            }
        }

        public string HankText(string text)
        {
            using (var image = ImageHelper.ProjectText(text, Path.Combine("assets", "images", "hank.json")))
            {
                string outputFilename = $"{Guid.NewGuid().ToString("N")}.jpg";
                image.Save(outputFilename);

                return outputFilename;
            }
        }

        private void EmmyImage(string imageFilename, uint numTimes = 1)
        {
            for (uint i = 0; i < numTimes; i++)
            {
                using (var image = ImageHelper.ProjectOnto(imageFilename, Path.Combine("assets", "images", "emmy.json")))
                {
                    image.Save(imageFilename);
                }
            }
        }

        public string EmmyText(string text)
        {
            using (var image = ImageHelper.ProjectText(text, Path.Combine("assets", "images", "emmy.json")))
            {
                string outputFilename = $"{Guid.NewGuid().ToString("N")}.jpg";
                image.Save(outputFilename);

                return outputFilename;
            }
        }
    }
}
