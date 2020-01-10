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
            using (var image = SixLabors.ImageSharp.Image.Load(imageFilename))
            {
                //calculate font size based on largest image dimension
                int fontSize = image.Width > image.Height ? image.Width / 12 : image.Height / 12;
                SixLabors.Fonts.Font font = SixLabors.Fonts.SystemFonts.CreateFont("Impact", fontSize);

                //compute text render size and font outline size
                SixLabors.Primitives.SizeF botTextSize = TextMeasurer.Measure(bottomText, new RendererOptions(font));
                float outlineSize = fontSize / 15.0f;

                //determine top & bottom text location
                float padding = 10f;
                float textMaxWidth = image.Width - (padding * 2);
                SixLabors.Primitives.PointF topLeftLocation = new SixLabors.Primitives.PointF(padding, padding);
                SixLabors.Primitives.PointF bottomLeftLocation = new SixLabors.Primitives.PointF(padding, image.Height - botTextSize.Height - padding * 2);

                //white brush for text fill and black pen for text outline
                SixLabors.ImageSharp.Processing.SolidBrush brush = new SixLabors.ImageSharp.Processing.SolidBrush(SixLabors.ImageSharp.Color.White);
                SixLabors.ImageSharp.Processing.Pen pen = new SixLabors.ImageSharp.Processing.Pen(SixLabors.ImageSharp.Color.Black, outlineSize);

                TextGraphicsOptions options = new TextGraphicsOptions()
                {
                    WrapTextWidth = textMaxWidth,
                    HorizontalAlignment = SixLabors.Fonts.HorizontalAlignment.Center,
                };

                //render text and save image
                if (!string.IsNullOrEmpty(topText))
                {
                    image.Mutate(x => x.DrawText(options, topText, font, brush, pen, topLeftLocation));
                }
                if (!string.IsNullOrEmpty(bottomText))
                {
                    image.Mutate(x => x.DrawText(options, bottomText, font, brush, pen, bottomLeftLocation));
                }
                image.Save(imageFilename);
            }
        }

        private void MorrowindImage(string imageFilename)
        {
            using (var image = SixLabors.ImageSharp.Image.Load(imageFilename))
            using (var morrowindImage = SixLabors.ImageSharp.Image.Load(Path.Combine("assets", "images", "morrowind.png")))
            {
                //resize the source image if it's too small to draw the morrowind dialogue on
                int resizeWidth = image.Width;
                int resizeHeight = image.Height;
                while (resizeWidth < morrowindImage.Width || resizeHeight < morrowindImage.Height)
                {
                    resizeWidth *= 2;
                    resizeHeight *= 2;
                }
                image.Mutate(x => x.Resize(resizeWidth, resizeHeight));

                //compute the position to draw the morrowind image at (based on its top-left corner)
                SixLabors.Primitives.Point position = new SixLabors.Primitives.Point(image.Width / 2 - morrowindImage.Width / 2, image.Height - morrowindImage.Height - image.Height / 10);

                image.Mutate(x => x.DrawImage(morrowindImage, position, 1.0f));
                image.Save(imageFilename);
            }
        }

        private void DMCWatermarkImage(string imageFilename)
        {
            using (var image = SixLabors.ImageSharp.Image.Load(imageFilename))
            using (var dmcImage = SixLabors.ImageSharp.Image.Load(Path.Combine("assets", "images", "dmc.png")))
            {
                //resize the source image if it's too small to draw the mDMC watermark on
                int resizeWidth = image.Width;
                int resizeHeight = image.Height;
                while (resizeWidth < dmcImage.Width || resizeHeight < dmcImage.Height)
                {
                    resizeWidth *= 2;
                    resizeHeight *= 2;
                }
                image.Mutate(x => x.Resize(resizeWidth, resizeHeight));

                //scale the DMC watermark so it's proportional in size to the source image
                dmcImage.Mutate(x => x.Resize(image.Height / 5, image.Height / 5));

                int paddingHorizontal = image.Width / 10;
                int paddingVertical = image.Height / 10;

                //compute the position to draw the morrowind image at (based on its top-left corner)
                SixLabors.Primitives.Point position = new SixLabors.Primitives.Point(image.Width - dmcImage.Width - paddingHorizontal, image.Height - dmcImage.Height - paddingVertical);

                image.Mutate(x => x.DrawImage(dmcImage, position, 0.8f));
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
