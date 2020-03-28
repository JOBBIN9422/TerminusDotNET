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

        public IConfiguration ClientSecrets { get; set; }

        public List<string> DeepfryImages(IReadOnlyCollection<Attachment> attachments, uint numPasses = 1)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                DeepfryImage(image, numPasses);
            }

            return images;
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
            using (var image = ImageHelper.WatermarkImage(imageFilename, Path.Combine("assets", "images", "morrowind.png"), AnchorPositionMode.Bottom, 10, 0.67))
            {
                image.Save(imageFilename);
            }
        }

        private void DMCWatermarkImage(string imageFilename)
        {
            using (var image = ImageHelper.WatermarkImage(imageFilename, Path.Combine("assets", "images", "dmc.png"), AnchorPositionMode.BottomRight, 10, 0.25))
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
