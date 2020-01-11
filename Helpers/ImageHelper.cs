using System;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.Fonts;
using System.IO;

namespace TerminusDotNetCore.Helpers
{
    public enum AnchorType
    {
        BottomCenter,
        BotttomRight
    }

    public static class ImageHelper
    {
        public static SixLabors.ImageSharp.Image DeepfryImage(string imageFilename, uint numPasses = 1)
        {
            using (var image = SixLabors.ImageSharp.Image.Load(imageFilename))
            using (var tempStream = new MemoryStream())
            {
                image.SaveAsJpeg(tempStream);

                var tempJpg = SixLabors.ImageSharp.Image.Load(tempStream.ToArray(), new JpegDecoder());
                for (uint i = 0; i < numPasses; i++)
                {
                    tempJpg.Mutate(x => x.Saturate(2.0f)
                                               .Contrast(2.0f)
                                               .GaussianSharpen());

                    //try to compress the image based on its file-type
                    tempJpg.SaveAsJpeg(tempStream, new JpegEncoder()
                    {
                        Quality = 10,
                    });
                }

                return tempJpg;
            }
        }

        public static SixLabors.ImageSharp.Image MemeCaptionImage(string imageFilename, string topText, string bottomText)
        {
            SixLabors.ImageSharp.Image image = SixLabors.ImageSharp.Image.Load(imageFilename);
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

            return image;
        }

        public static void ResizeProportional(this SixLabors.ImageSharp.Image image, double scaleFactor)
        {
            double aspectRatio = image.Width / (double)image.Height;

            int resizeX;
            int resizeY;

            //compute new watermark size based on whether or not the watermark image is portrait or landscape 
            if (image.Width > image.Height)
            {
                resizeY = (int)(image.Height * scaleFactor);
                resizeX = (int)(resizeY * aspectRatio);
            }
            else
            {
                resizeX = (int)(image.Width * scaleFactor);
                resizeY = (int)(resizeX * aspectRatio);
            }

            image.Mutate(x => x.Resize(resizeX, resizeY));
        }

        public static SixLabors.ImageSharp.Image WatermarkImage(string baseImageFilename, string watermarkImageFilename, AnchorPositionMode anchorPos = AnchorPositionMode.Bottom, int paddingScale = 10, double watermarkScale = 0.2)
        {
            var baseImage = SixLabors.ImageSharp.Image.Load(baseImageFilename);
            using (var watermarkImage = SixLabors.ImageSharp.Image.Load(watermarkImageFilename))
            {
                //resize the source image if it's too small to draw the watermark on
                int resizeWidth = baseImage.Width;
                int resizeHeight = baseImage.Height;
                while (resizeWidth < watermarkImage.Width || resizeHeight < watermarkImage.Height)
                {
                    resizeWidth *= 2;
                    resizeHeight *= 2;
                }
                baseImage.Mutate(x => x.Resize(resizeWidth, resizeHeight));

                //scale the watermark so it's proportional in size to the source image
                double scaleFactor = baseImage.Width / (double)watermarkImage.Width * watermarkScale;
                watermarkImage.ResizeProportional(scaleFactor);

                //compute padding
                int paddingHorizontal = baseImage.Width / paddingScale;
                int paddingVertical = baseImage.Height / paddingScale;

                //compute the position to draw the watermark at (based on its top-left corner)
                SixLabors.Primitives.Point position;
                switch (anchorPos)
                {
                    case AnchorPositionMode.BottomRight:
                        position = new SixLabors.Primitives.Point(baseImage.Width - watermarkImage.Width - paddingHorizontal, baseImage.Height - watermarkImage.Height - paddingVertical);
                        break;

                    case AnchorPositionMode.Bottom:
                        position = new SixLabors.Primitives.Point(baseImage.Width / 2 - watermarkImage.Width / 2, baseImage.Height - watermarkImage.Height - paddingVertical);
                        break;

                    default:
                        position = new SixLabors.Primitives.Point(0, 0);
                        break;

                }

                //draw the watermark on the base image with 80% opacity
                baseImage.Mutate(x => x.DrawImage(watermarkImage, position, 0.8f));

                return baseImage;
            }
        }

        public static SixLabors.ImageSharp.Image ThiccImage(string imageFilename, int thiccCount)
        {
            var image = SixLabors.ImageSharp.Image.Load(imageFilename);
            int originalWidth = image.Width;
            int originalHeight = image.Height;

            image.Mutate(x => x.Resize(thiccCount * originalWidth, image.Height));
            return image;
        }

        public static SixLabors.ImageSharp.Image MosaicImage(string baseImageFilename, string tileImageFilename, double tileScaleFactor = 0.01)
        {
            var outputImage = SixLabors.ImageSharp.Image.Load<Rgba32>(baseImageFilename);
            using (var tileImage = SixLabors.ImageSharp.Image.Load<Rgba32>(tileImageFilename))
            {
                tileImage.ResizeProportional(outputImage.Width / (double)tileImage.Width * tileScaleFactor);

                for (int y = 0; y < outputImage.Height; y += tileImage.Height)
                {
                    for (int x = 0; x < outputImage.Width; x += tileImage.Width)
                    {
                        SixLabors.Primitives.Point tileUpperLeftLoc = new SixLabors.Primitives.Point(x, y);
                        Rgba32 avgColor = GetAverageColor(outputImage, x, y, tileImage.Width, tileImage.Height);
                        outputImage.Mutate(i => i.Fill(avgColor, new SixLabors.Primitives.Rectangle(x, y, tileImage.Width, tileImage.Height)));
                        outputImage.Mutate(i => i.DrawImage(tileImage, tileUpperLeftLoc, PixelColorBlendingMode.Normal, 0.5f));
                    }
                }
            }

            return outputImage;
        }

        public static Rgba32 GetAverageColor(SixLabors.ImageSharp.Image<Rgba32> inputImage, int startX, int startY, int width, int height)
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
                    Rgba32 currColor = inputImage[x, y];

                    //sum of squares for each color value
                    red += currColor.R * currColor.R;
                    blue += currColor.B * currColor.B;
                    green += currColor.G * currColor.G;

                    numPixels++;
                }
            }

            Rgba32 avgColor = new Rgba32(
                    (float)Math.Sqrt(red / numPixels)   / 255.0f,
                    (float)Math.Sqrt(green / numPixels) / 255.0f,
                    (float)Math.Sqrt(blue / numPixels)  / 255.0f,
                1);
            return avgColor;
        }

        private static System.Drawing.Color BlendColor(System.Drawing.Color baseColor, System.Drawing.Color blendColor, double amount)
        {
            //blend the argument color into the base color by the given amount
            byte r = (byte)((blendColor.R * amount) + baseColor.R * (1 - amount));
            byte g = (byte)((blendColor.G * amount) + baseColor.G * (1 - amount));
            byte b = (byte)((blendColor.B * amount) + baseColor.B * (1 - amount));

            return System.Drawing.Color.FromArgb(r, g, b);
        }

        public static SixLabors.ImageSharp.Image<Rgba32> ProjectOnto(string projectImageFilename, string baseImageFilename,
            SixLabors.Primitives.Point topLeft,
            SixLabors.Primitives.Point topRight,
            SixLabors.Primitives.Point bottomLeft,
            SixLabors.Primitives.Point bottomRight)
        {
            using (var projectImage = SixLabors.ImageSharp.Image.Load(projectImageFilename))
            using (var baseImage = SixLabors.ImageSharp.Image.Load(baseImageFilename))
            {
                //declare without using statement (need to return this so can't dispose of it)
                var outputImage = new Image<Rgba32>(baseImage.Width, baseImage.Height);

                //compute the transformation matrix based on the destination points and apply it to the input image
                Matrix4x4 transformMat = TransformHelper.ComputeTransformMatrix(projectImage.Width, projectImage.Height, topLeft, topRight, bottomLeft, bottomRight);

                //project the image according to the input points (and realign it to fix any EXIF bugs)
                projectImage.Mutate(x => x.AutoOrient());
                projectImage.Mutate(x => x.Transform(new ProjectiveTransformBuilder().AppendMatrix(transformMat)));

                //draw the base image on top of the projected image
                outputImage.Mutate(x => x.DrawImage(projectImage, 1.0f));
                outputImage.Mutate(x => x.DrawImage(baseImage, new SixLabors.Primitives.Point(0, 0), 1.0f));

                return outputImage;
            }
        }

        public static string ProjectText(string text, string baseImageFilename,
            SixLabors.Primitives.Point topLeft,
            SixLabors.Primitives.Point topRight,
            SixLabors.Primitives.Point bottomLeft,
            SixLabors.Primitives.Point bottomRight)
        {
            using (var baseImage = SixLabors.ImageSharp.Image.Load(baseImageFilename))
            using (var textImage = new Image<Rgba32>(1920, 1080))
            using (var outputImage = new Image<Rgba32>(baseImage.Width, baseImage.Height))

            {
                int fontSize = textImage.Width / 10;
                SixLabors.Fonts.Font font = SixLabors.Fonts.SystemFonts.CreateFont("Impact", fontSize);

                //determine text location
                float padding = 10f;
                float textMaxWidth = textImage.Width - (padding * 2);
                SixLabors.Primitives.PointF topLeftLocation = new SixLabors.Primitives.PointF(padding, padding * 2);

                //black brush for text fill
                SixLabors.ImageSharp.Processing.SolidBrush brush = new SixLabors.ImageSharp.Processing.SolidBrush(SixLabors.ImageSharp.Color.Black);

                //wrap and align text before drawing
                TextGraphicsOptions options = new TextGraphicsOptions()
                {
                    WrapTextWidth = textMaxWidth,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };

                //draw text on the text canvas
                textImage.Mutate(x => x.BackgroundColor(SixLabors.ImageSharp.Color.White));
                textImage.Mutate(x => x.DrawText(options, text, font, brush, topLeftLocation));

                //compute the transformation matrix based on the destination points and apply it to the text image
                Matrix4x4 transformMat = TransformHelper.ComputeTransformMatrix(textImage.Width, textImage.Height, topLeft, topRight, bottomLeft, bottomRight);
                textImage.Mutate(x => x.Transform(new ProjectiveTransformBuilder().AppendMatrix(transformMat)));

                //draw the projected text and the base image on the output image
                outputImage.Mutate(x => x.DrawImage(textImage, new SixLabors.Primitives.Point(0, 0), 1.0f));
                outputImage.Mutate(x => x.DrawImage(baseImage, 1.0f));

                string outputFilename = $"{Guid.NewGuid().ToString("N")}.jpg";
                outputImage.Save(outputFilename);

                return outputFilename;
            }
        }
    }
}
