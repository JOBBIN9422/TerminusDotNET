using System;
using System.Collections.Generic;
using System.IO;
using Discord;
using TerminusDotNetCore.Modules;
using System.Drawing;
using ImageProcessor.Imaging.Formats;
using ImageProcessor;
using ImageProcessor.Imaging;
using TerminusDotNetCore.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using ImageProcessor.Processors;
using System.Numerics;
using SixLabors.Fonts;

namespace TerminusDotNetCore.Services
{
    public class ImageService : ICustomService
    {
        public IServiceModule ParentModule { get; set; }

        public List<string> DeepfryImages(IReadOnlyCollection<Attachment> attachments, int numPasses = 1)
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

        private void DeepfryImage(string imageFilename, int numPasses = 1)
        {
            for (int i = 0; i < numPasses; i++)
            {
                using (var image = SixLabors.ImageSharp.Image.Load(imageFilename))
                {
                    image.Mutate(x => x.Saturate(2.0f)
                                       .Contrast(2.0f)
                                       .GaussianSharpen());

                    //try to compress the image based on its file-type
                    string extenstion = Path.GetExtension(imageFilename);
                    switch (extenstion)
                    {
                        case ".jpeg":
                        case ".jpg":
                            image.Save(imageFilename, new JpegEncoder()
                            {
                                Quality = 10,
                            });
                            break;

                        case ".png":
                            image.Save(imageFilename, new PngEncoder()
                            {
                                CompressionLevel = 9
                            });
                            break;

                        default:
                            image.Save(imageFilename);
                            break;
                    }
                }
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
                image.Mutate(x => x.DrawText(options, topText, font, brush, pen, topLeftLocation)
                                   .DrawText(options, bottomText, font, brush, pen, bottomLeftLocation));
                image.Save(imageFilename);
            }
        }

        private void MorrowindImage(string imageFilename)
        {
            using (var image = SixLabors.ImageSharp.Image.Load(imageFilename))
            using (var morrowindImage = SixLabors.ImageSharp.Image.Load("morrowind.png"))
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
            using (var dmcImage = SixLabors.ImageSharp.Image.Load("dmc.png"))
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
                int paddingVertical   = image.Height / 10;

                //compute the position to draw the morrowind image at (based on its top-left corner)
                SixLabors.Primitives.Point position = new SixLabors.Primitives.Point(image.Width  - dmcImage.Width - paddingHorizontal, image.Height - dmcImage.Height - paddingVertical);

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
            using (Bitmap pepperImage = new Bitmap("GIMP_Pepper.png"))
            {
                //calculate the average RGB value of each pepper-sized cell of the input image 
                for (int y = 0; y < inputImage.Height; y += pepperImage.Height)
                {
                    List<System.Drawing.Color> rowAvgColors = new List<System.Drawing.Color>();
                    for (int x = 0; x < inputImage.Width; x += pepperImage.Width)
                    {
                        rowAvgColors.Add(GetAverageColor(inputImage, x, y, pepperImage.Width, pepperImage.Height));
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
                                using (Bitmap blendedPepperImage = new Bitmap("GIMP_Pepper.png"))
                                {
                                    BlendImage(blendedPepperImage, avgColors[y][x], 0.5);
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

        public List<string> BobRossImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = AttachmentHelper.DownloadAttachments(attachments);

            foreach (var image in images)
            {
                BobRossImage(image);
            }

            return images;
        }

        private void BobRossImage(string imageFilename)
        {
            using (var projectImage = SixLabors.ImageSharp.Image.Load(imageFilename))
            using (var bobRossImage = SixLabors.ImageSharp.Image.Load("bobross.jpg"))
            {
                //define projection points for the corners of Bob's happy little canvas
                SixLabors.Primitives.Point topLeft = new SixLabors.Primitives.Point(297, 22);
                SixLabors.Primitives.Point topRight = new SixLabors.Primitives.Point(490, 5);
                SixLabors.Primitives.Point bottomRight = new SixLabors.Primitives.Point(493, 213);
                SixLabors.Primitives.Point bottomLeft = new SixLabors.Primitives.Point(304, 194);

                //compute the transformation matrix based on the destination points and apply it to the input image
                Matrix4x4 transformMat = TransformHelper.ComputeTransformMatrix(projectImage.Width, projectImage.Height, topLeft, topRight, bottomLeft, bottomRight);
                projectImage.Mutate(x => x.AutoOrient());
                projectImage.Mutate(x => x.Transform(new ProjectiveTransformBuilder().AppendMatrix(transformMat)));
                bobRossImage.Mutate(x => x.DrawImage(projectImage, new SixLabors.Primitives.Point(0, 0), 1.0f));
                bobRossImage.Save(imageFilename);
            }

        }

        public string BobRossText(string text)
        {
            using (var bobRossImage = SixLabors.ImageSharp.Image.Load("bobross.jpg"))
            using (var textImage = new Image<Rgba32>(1920, 1080))
            {
                //define projection points for the corners of Bob's happy little canvas
                SixLabors.Primitives.Point topLeft = new SixLabors.Primitives.Point(297, 22);
                SixLabors.Primitives.Point topRight = new SixLabors.Primitives.Point(490, 5);
                SixLabors.Primitives.Point bottomRight = new SixLabors.Primitives.Point(493, 213);
                SixLabors.Primitives.Point bottomLeft = new SixLabors.Primitives.Point(304, 194);

                int fontSize = textImage.Width / 10;
                SixLabors.Fonts.Font font = SixLabors.Fonts.SystemFonts.CreateFont("Impact", fontSize);

                //determine text location
                float padding = 10f;
                float textMaxWidth = textImage.Width - (padding * 2);
                SixLabors.Primitives.PointF topLeftLocation = new SixLabors.Primitives.PointF(padding, padding * 2);

                //black brush for text fill
                SixLabors.ImageSharp.Processing.SolidBrush brush = new SixLabors.ImageSharp.Processing.SolidBrush(SixLabors.ImageSharp.Color.Black);

                TextGraphicsOptions options = new TextGraphicsOptions()
                {
                    WrapTextWidth = textMaxWidth,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };

                textImage.Mutate(x => x.DrawText(options, text, font, brush, topLeftLocation));

                //compute the transformation matrix based on the destination points and apply it to the text image
                Matrix4x4 transformMat = TransformHelper.ComputeTransformMatrix(textImage.Width, textImage.Height, topLeft, topRight, bottomLeft, bottomRight);
                textImage.Mutate(x => x.Transform(new ProjectiveTransformBuilder().AppendMatrix(transformMat)));
                bobRossImage.Mutate(x => x.DrawImage(textImage, new SixLabors.Primitives.Point(0, 0), 1.0f));

                string outputFilename = $"{Guid.NewGuid().ToString("N")}.jpg";
                bobRossImage.Save(outputFilename);

                return outputFilename;
            }
        }

        private System.Drawing.Color GetAverageColor(System.Drawing.Bitmap inputImage, int startX, int startY, int width, int height)
        {
            //prevent going out of bounds during calculations
            int maxX = startX + width;
            int maxY = startY + height;
            if (startX + width > inputImage.Width)
            {
                maxX = inputImage.Width;
            }
            if (startY + height > inputImage.Height)
            {
                maxY = inputImage.Height;
            }

            //average RGB values
            double red = 0;
            double blue = 0;
            double green = 0;

            //how many pixels we've calculated
            int numPixels = 0;

            for (int y = startY; y < maxY; y++)
            {
                for (int x = startX; x < maxX; x++)
                {
                    System.Drawing.Color currColor = inputImage.GetPixel(x, y);

                    //sum of squares for each color value
                    red += currColor.R * currColor.R;
                    blue += currColor.B * currColor.B;
                    green += currColor.G * currColor.G;

                    numPixels++;
                }
            }

            System.Drawing.Color avgColor = System.Drawing.Color.FromArgb(255,
                (int)Math.Sqrt(red / numPixels),
                (int)Math.Sqrt(green / numPixels),
                (int)Math.Sqrt(blue / numPixels));
            return avgColor;
        }

        private void BlendImage(Bitmap image, System.Drawing.Color blendColor, double amount)
        {
            //blend the argument color into each pixel of the source image 
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    System.Drawing.Color blendedColor = BlendColor(image.GetPixel(x, y), blendColor, amount);
                    image.SetPixel(x, y, blendedColor);
                }
            }
        }


        private System.Drawing.Color BlendColor(System.Drawing.Color baseColor, System.Drawing.Color blendColor, double amount)
        {
            //blend the argument color into the base color by the given amount
            byte r = (byte)((blendColor.R * amount) + baseColor.R * (1 - amount));
            byte g = (byte)((blendColor.G * amount) + baseColor.G * (1 - amount));
            byte b = (byte)((blendColor.B * amount) + baseColor.B * (1 - amount));

            return System.Drawing.Color.FromArgb(r, g, b);
        }
    }
}
