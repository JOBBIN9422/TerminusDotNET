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
using System.Threading.Tasks;

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
                DeepfryImage(image, numPasses);
            }

            return images;
        }

        public List<string> GrayscaleImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                GrayscaleImage(image);
            }

            return images;
        }

        public List<string> PolaroidImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                PolaroidImage(image);
            }

            return images;
        }

        public List<string> InvertImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                InvertImage(image);
            }

            return images;
        }

        public List<string> KodakImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                KodakImage(image);
            }

            return images;
        }

        private void KodakImage(string imageFilename)
        {
            using (var image = ImageHelper.KodakImage(imageFilename))
            {
                image.Save(imageFilename);
            }
        }

        private void InvertImage(string imageFilename)
        {
            using (var image = ImageHelper.InvertImage(imageFilename))
            {
                image.Save(imageFilename);
            }
        }

        public List<string> PixelateImages(IReadOnlyCollection<Attachment> attachments, int size)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                PixelateImage(image, size);
            }

            return images;
        }

        public List<string> ContrastImages(IReadOnlyCollection<Attachment> attachments, float amount)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                ContrastImage(image, amount);
            }

            return images;
        }

        public List<string> SaturateImages(IReadOnlyCollection<Attachment> attachments, float amount)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                SaturateImage(image, amount);
            }

            return images;
        }

        private void SaturateImage(string imageFilename, float amount)
        {
            using (var image = ImageHelper.SaturateImage(imageFilename, amount))
            {
                image.Save(imageFilename);
            }
        }

        private void ContrastImage(string imageFilename, float amount)
        {
            using (var image = ImageHelper.ContrastImage(imageFilename, amount))
            {
                image.Save(imageFilename);
            }
        }

        private void PixelateImage(string imageFilename, int size)
        {
            using (var image = ImageHelper.PixelateImage(imageFilename, size))
            {
                image.Save(imageFilename);
            }
        }

        private void PolaroidImage(string imageFilename)
        {
            using (var image = ImageHelper.PolaroidImage(imageFilename))
            {
                image.Save(imageFilename);
            }
        }

        private void GrayscaleImage(string imageFilename)
        {
            using (var image = ImageHelper.GrayscaleImage(imageFilename))
            {
                image.Save(imageFilename);
            }
        }

        public void DeleteImages(List<string> images)
        {
            AttachmentHelper.DeleteFiles(images);
        }

        private void DeepfryImage(string imageFilename, uint numPasses = 1)
        {
            using (var image = ImageHelper.DeepfryImage(imageFilename, numPasses))
            {
                image.Save(imageFilename);
            }
        }

        public List<string> MorrowindImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                MorrowindImage(image);
            }

            return images;
        }

        public List<string> DMCWatermarkImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                DMCWatermarkImage(image);
            }

            return images;
        }

        public List<string> BebopWatermarkImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                BebopWatermarkImage(image);
            }

            return images;
        }

        public List<string> NintendoWatermarkImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                NintendoWatermarkImage(image);
            }

            return images;
        }

        public List<string> MemeCaptionImages(IReadOnlyCollection<Attachment> attachments, string topText, string bottomText)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                MemeCaptionImage(image, topText, bottomText);
            }

            return images;
        }

        private void MemeCaptionImage(string imageFilename, string topText, string bottomText)
        {
            using (var image = ImageHelper.MemeCaptionImage(imageFilename, topText, bottomText))
            {
                image.Save(imageFilename);
            }
        }

        private void MorrowindImage(string imageFilename)
        {
            using (var image = ImageHelper.WatermarkImage(imageFilename, Path.Combine("assets", "images", "morrowind.png"), AnchorPositionMode.Bottom, 0.1, 0.1, 0.67))
            {
                image.Save(imageFilename);
            }
        }

        private void DMCWatermarkImage(string imageFilename)
        {
            using (var image = ImageHelper.WatermarkImage(imageFilename, Path.Combine("assets", "images", "dmc.png"), AnchorPositionMode.BottomLeft, 0.1, 0.1, 0.25))
            {
                image.Save(imageFilename);
            }
        }

        private void BebopWatermarkImage(string imageFilename)
        {
            using (var image = ImageHelper.WatermarkImage(imageFilename, Path.Combine("assets", "images", "bebop.png"), AnchorPositionMode.BottomRight, 0.05, 0.05, 0.6, 1.0f))
            {
                image.Save(imageFilename);
            }
        }

        private void NintendoWatermarkImage(string imageFilename)
        {
            using (var image = ImageHelper.WatermarkImage(imageFilename, Path.Combine("assets", "images", "nintendo.png"), AnchorPositionMode.BottomRight, 0.1, 0.1, 0.25))
            {
                image.Save(imageFilename);
            }
        }

        public List<string> ThiccImages(IReadOnlyCollection<Attachment> attachments, int thiccCount = 2)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                ThiccImage(image, thiccCount);
            }

            return images;
        }

        private void ThiccImage(string filename, int thiccCount)
        {
            using (var image = ImageHelper.ThiccImage(filename, thiccCount))
            {
                image.Save(filename);
            }
        }

        public List<string> MosaicImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                MosaicImage(image);
            }

            return images;
        }

        private void MosaicImage(string filename)
        {
            using (var image = ImageHelper.MosaicImage(filename, Path.Combine("assets", "images", "GIMP_Pepper.png"), 0.02, 0.5f))
            {
                image.Save(filename);
            }
        }

        public List<string> BobRossImages(IReadOnlyCollection<Attachment> attachments, uint numTimes = 1)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                BobRossImage(image, numTimes);
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

        private void BobRossImage(string imageFilename, uint numTimes = 1)
        {
            for (uint i = 0; i < numTimes; i++)
            {
                using (var image = ImageHelper.ProjectOnto(imageFilename, Path.Combine("assets", "images", "bobross.json")))
                {
                    image.Save(imageFilename);
                }
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
                PCImage(image, numTimes);
            }

            return images;
        }

        private void PCImage(string imageFilename, uint numTimes = 1)
        {
            for (uint i = 0; i < numTimes; i++)
            {
                using (var image = ImageHelper.ProjectOnto(imageFilename, Path.Combine("assets", "images", "pc.json")))
                {
                    image.Save(imageFilename);
                }
            }
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
                WalterImage(image, numTimes);
            }

            return images;
        }

        public async Task<List<string>> GenerateAiPortaitAsync(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);
            foreach (var image in images)
            {
                await PortraitAiClient.PostImage(image);
            }
            return images;
        }

        public List<string> TrumpImages(IReadOnlyCollection<Attachment> attachments, uint numTimes = 1)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                TrumpImage(image, numTimes);
            }

            return images;
        }

        public List<string> HankImages(IReadOnlyCollection<Attachment> attachments, uint numTimes = 1)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                HankImage(image, numTimes);
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
    }
}
