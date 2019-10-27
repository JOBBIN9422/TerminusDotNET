using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord;
using TerminusDotNetCore.Modules;
using System.Drawing;
using ImageProcessor.Imaging.Formats;
using ImageProcessor;
using ImageProcessor.Imaging;
using TerminusDotNetCore.Helpers;

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
                var imageBytes = File.ReadAllBytes(imageFilename);
                var format = new JpegFormat { Quality = 10 };

                using (var inStream = new MemoryStream(imageBytes))
                using (var outStream = new MemoryStream())
                using (var saveFileStream = new FileStream(imageFilename, FileMode.Open, FileAccess.Write))
                using (var imageFactory = new ImageFactory(preserveExifData: true))
                {
                    imageFactory.Load(inStream)
                                .Saturation(100)
                                .Contrast(100)
                                .Gamma(1.0f)
                                //.GaussianSharpen(30)
                                .Format(format)
                                .Save(outStream);
                    outStream.CopyTo(saveFileStream);
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
            var imageBytes = File.ReadAllBytes(imageFilename);
            var format = new JpegFormat { Quality = 10 };

            var imageWidth = 0;
            var imageHeight = 0;
            using (var imageBmp = new Bitmap(imageFilename))
            {
                imageWidth = imageBmp.Width;
                imageHeight = imageBmp.Height;
            }

            var fontSize = 0;
            if (imageWidth > imageHeight)
            {
                fontSize = imageWidth / 15;
            }
            else
            {
                fontSize = imageHeight / 15;
            }

            var topTextX = imageWidth / 2 - topText.Length * (fontSize / 3);
            var bottomTextX = imageWidth / 2 - bottomText.Length * (fontSize / 3);

            var topTextLayer = new TextLayer()
            {
                Text = topText,
                FontFamily = new FontFamily("Impact"),
                FontSize = fontSize,
                FontColor = System.Drawing.Color.White,
                Style = FontStyle.Bold,
                Position = new Point(topTextX, imageHeight / 10)

            };

            var bottomTextLayer = new TextLayer()
            {
                Text = bottomText,
                FontFamily = new FontFamily("Impact"),
                FontSize = fontSize,
                FontColor = System.Drawing.Color.White,
                Style = FontStyle.Bold,
                Position = new Point(bottomTextX, imageHeight * 8 / 10)
            };

            using (var inStream = new MemoryStream(imageBytes))
            using (var outStream = new MemoryStream())
            using (var saveFileStream = new FileStream(imageFilename, FileMode.Open, FileAccess.Write))
            using (var imageFactory = new ImageFactory(preserveExifData: true))
            {
                imageFactory.Load(inStream)
                            .Watermark(topTextLayer)
                            .Watermark(bottomTextLayer)
                            .Save(outStream);
                outStream.CopyTo(saveFileStream);
            }
        }

        private void MorrowindImage(string imageFilename)
        {
            var morrowindImage = System.Drawing.Image.FromFile("morrowind.png");

            //forward declares (need outside of using block)
            System.Drawing.Image resizeImage;
            byte[] imageBytes;
            ImageLayer morrowindLayer;

            //using (var baseImage = System.Drawing.Image.FromFile(imageFilename))
            using (var baseImageStream = new FileStream(imageFilename, FileMode.Open, FileAccess.Read))
            {
                var baseImage = System.Drawing.Image.FromStream(baseImageStream);

                //resize the image if it's smaller than the Morrowind dialogue image 
                resizeImage = (System.Drawing.Image)baseImage.Clone();
                int resizeWidth = resizeImage.Width;
                int resizeHeight = resizeImage.Height;
                while (resizeWidth < morrowindImage.Width || resizeHeight < morrowindImage.Height)
                {
                    resizeWidth *= 2;
                    resizeHeight *= 2;
                    //resizeImage = (System.Drawing.Image)new Bitmap(resizeImage, new Size(resizeImage.Width * 2, resizeImage.Height * 2));
                }
                resizeImage = (System.Drawing.Image)new Bitmap(resizeImage, new Size(resizeWidth, resizeHeight));

                //get image data after resize
                var converter = new ImageConverter();
                imageBytes = (byte[])converter.ConvertTo(resizeImage, typeof(byte[]));


                //apply Morrowind dialogue box as a new layer
                morrowindLayer = new ImageLayer()
                {
                    Image = morrowindImage,

                    //center horizontally and position towards bottom of base image
                    Position = new Point(resizeImage.Width / 2 - morrowindImage.Width / 2, resizeImage.Height - morrowindImage.Height - resizeImage.Height / 10)
                };
                resizeImage.Dispose();
            }


            using (var inStream = new MemoryStream(imageBytes))
            using (var outStream = new MemoryStream())
            using (var saveFileStream = new FileStream(imageFilename, FileMode.Open, FileAccess.Write))
            using (var imageFactory = new ImageFactory(preserveExifData: true))
            {
                imageFactory.Load(inStream)
                            .Overlay(morrowindLayer)
                            .Save(outStream);
                outStream.CopyTo(saveFileStream);
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
            int imgWidth = 0;
            int imgHeight = 0;
            using (var image = System.Drawing.Image.FromFile(filename))
            {
                imgWidth = image.Width;
                imgHeight = image.Height;
            }
            var imageBytes = File.ReadAllBytes(filename);

            using (var inStream = new MemoryStream(imageBytes))
            using (var outStream = new MemoryStream())
            using (var saveFileStream = new FileStream(filename, FileMode.Open, FileAccess.Write))
            using (var imageFactory = new ImageFactory(preserveExifData: true))
            {
                imageFactory.Load(inStream)
                            .Resize(new ResizeLayer(
                                new Size(
                                    imgWidth * thiccCount, imgHeight),
                                ResizeMode.Stretch,
                                AnchorPosition.Center,
                                true)
                            )
                            .Resolution(imgWidth * thiccCount, imgHeight)
                            .Save(outStream);
                outStream.CopyTo(saveFileStream);
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
                            using (SolidBrush brush = new SolidBrush(avgColors[y][x]))
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