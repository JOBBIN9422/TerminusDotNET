﻿using System;
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

namespace TerminusDotNetCore.Services
{
    public class ImageService : ICustomService
    {
        public ServiceControlModule ParentModule { get; set; }

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
            using (var image = ImageHelper.WatermarkImage(imageFilename, Path.Combine("assets", "images", "morrowind.png"), AnchorPositionMode.Bottom, 10, 5))
            {
                image.Save(imageFilename);
            }
        }

        private void DMCWatermarkImage(string imageFilename)
        {
            using (var image = ImageHelper.WatermarkImage(imageFilename, Path.Combine("assets", "images", "dmc.png"), AnchorPositionMode.BottomRight, 10, 4))
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
            using (var image = SixLabors.ImageSharp.Image.Load(filename))
            {
                int originalWidth = image.Width;
                int originalHeight = image.Height;

                image.Mutate(x => x.Resize(thiccCount * originalWidth, image.Height));
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
            //will contain average color of each pepper-sized square of the input image 
            List<List<System.Drawing.Color>> avgColors = new List<List<System.Drawing.Color>>();

            //will contain the raw image data of the input image 
            MemoryStream memStream = new MemoryStream();


            using (System.Drawing.Bitmap inputImage = new Bitmap(filename))
            using (Bitmap pepperImage = new Bitmap(Path.Combine("assets", "images", "GIMP_Pepper.png")))
            {
                //calculate the average RGB value of each pepper-sized cell of the input image 
                for (int y = 0; y < inputImage.Height; y += pepperImage.Height)
                {
                    List<System.Drawing.Color> rowAvgColors = new List<System.Drawing.Color>();
                    for (int x = 0; x < inputImage.Width; x += pepperImage.Width)
                    {
                        rowAvgColors.Add(ImageHelper.GetAverageColor(inputImage, x, y, pepperImage.Width, pepperImage.Height));
                    }
                    avgColors.Add(rowAvgColors);
                }

                //draw a pepper blended with the average color for each 'cell' of the source image
                using (Graphics graphics = Graphics.FromImage(inputImage))
                {
                    for (int y = 0; y < avgColors.Count; y++)
                    {
                        for (int x = 0; x < avgColors[y].Count; x++)
                        {
                            using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(avgColors[y][x]))
                            {
                                using (Bitmap blendedPepperImage = new Bitmap(Path.Combine("assets", "images", "GIMP_Pepper.png")))
                                {
                                    ImageHelper.BlendImage(blendedPepperImage, avgColors[y][x], 0.5);
                                    graphics.DrawImage(blendedPepperImage, x * pepperImage.Width, y * pepperImage.Height);
                                }
                            }
                        }
                    }
                    //dump the modified image data to mem stream
                    inputImage.Save(memStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
            }

            try
            {
                using (System.Drawing.Image saveImage = System.Drawing.Image.FromStream(memStream))
                {
                    saveImage.Save(filename);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
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
            //define projection points for the corners of Bob's happy little canvas
            SixLabors.Primitives.Point topLeft = new SixLabors.Primitives.Point(24, 72);
            SixLabors.Primitives.Point topRight = new SixLabors.Primitives.Point(451, 91);
            SixLabors.Primitives.Point bottomRight = new SixLabors.Primitives.Point(437, 407);
            SixLabors.Primitives.Point bottomLeft = new SixLabors.Primitives.Point(23, 388);


            for (int i = 0; i < numTimes; i++)
            {
                using (var outputImage = ImageHelper.ProjectOnto(imageFilename, Path.Combine("assets", "images", "bobross.png"), topLeft, topRight, bottomLeft, bottomRight))
                {
                    outputImage.Save(imageFilename);
                }
            }
        }

        public string BobRossText(string text)
        {

            //define projection points for the corners of Bob's happy little canvas
            SixLabors.Primitives.Point topLeft = new SixLabors.Primitives.Point(24, 72);
            SixLabors.Primitives.Point topRight = new SixLabors.Primitives.Point(451, 91);
            SixLabors.Primitives.Point bottomRight = new SixLabors.Primitives.Point(437, 407);
            SixLabors.Primitives.Point bottomLeft = new SixLabors.Primitives.Point(23, 388);

            return ImageHelper.ProjectText(text, Path.Combine("assets", "images", "bobross.png"), topLeft, topRight, bottomLeft, bottomRight);

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
            SixLabors.Primitives.Point topLeft = new SixLabors.Primitives.Point(69, 334);
            SixLabors.Primitives.Point topRight = new SixLabors.Primitives.Point(335, 292);
            SixLabors.Primitives.Point bottomRight = new SixLabors.Primitives.Point(432, 579);
            SixLabors.Primitives.Point bottomLeft = new SixLabors.Primitives.Point(214, 726);

            for (int i = 0; i < numTimes; i++)
            {
                using (var outputImage = ImageHelper.ProjectOnto(imageFilename, Path.Combine("assets", "images", "suicide.png"), topLeft, topRight, bottomLeft, bottomRight))
                {
                    outputImage.Save(imageFilename);
                }
            }
        }

        public string PCText(string text)
        {
            SixLabors.Primitives.Point topLeft = new SixLabors.Primitives.Point(69, 334);
            SixLabors.Primitives.Point topRight = new SixLabors.Primitives.Point(335, 292);
            SixLabors.Primitives.Point bottomRight = new SixLabors.Primitives.Point(432, 579);
            SixLabors.Primitives.Point bottomLeft = new SixLabors.Primitives.Point(214, 726);

            return ImageHelper.ProjectText(text, Path.Combine("assets", "images", "suicide.png"), topLeft, topRight, bottomLeft, bottomRight);
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

        private void TrumpImage(string imageFilename, uint numTimes = 1)
        {
            //defince projection points for corners of book
            SixLabors.Primitives.Point topLeft = new SixLabors.Primitives.Point(218, 164);
            SixLabors.Primitives.Point topRight = new SixLabors.Primitives.Point(366, 164);
            SixLabors.Primitives.Point bottomRight = new SixLabors.Primitives.Point(368, 361);
            SixLabors.Primitives.Point bottomLeft = new SixLabors.Primitives.Point(220, 365);

            for (int i = 0; i < numTimes; i++)
            {
                using (var outputImage = ImageHelper.ProjectOnto(imageFilename, Path.Combine("assets", "images", "don.png"), topLeft, topRight, bottomLeft, bottomRight))
                {
                    outputImage.Save(imageFilename);
                }
            }
        }

        public string TrumpText(string text)
        {
            SixLabors.Primitives.Point topLeft = new SixLabors.Primitives.Point(218, 164);
            SixLabors.Primitives.Point topRight = new SixLabors.Primitives.Point(366, 164);
            SixLabors.Primitives.Point bottomRight = new SixLabors.Primitives.Point(368, 361);
            SixLabors.Primitives.Point bottomLeft = new SixLabors.Primitives.Point(220, 365);

            return ImageHelper.ProjectText(text, Path.Combine("assets", "images", "don.png"), topLeft, topRight, bottomLeft, bottomRight);
        }
    }
}
