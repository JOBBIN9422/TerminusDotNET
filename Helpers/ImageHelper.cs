using System;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.Primitives;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.Fonts;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace TerminusDotNetCore.Helpers
{
    public enum AnchorType
    {
        BottomCenter,
        BotttomRight
    }

    public static class ImageHelper
    {
        public static Image DeepfryImage(string imageFilename, uint numPasses = 1)
        {
            using (var image = Image.Load(imageFilename))
            using (var tempStream = new MemoryStream())
            {
                image.SaveAsJpeg(tempStream);

                var tempJpg = Image.Load(tempStream.ToArray(), new JpegDecoder());
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

        public static Image MemeCaptionImage(string imageFilename, string topText, string bottomText)
        {
            Image image = Image.Load(imageFilename);
            //calculate font size based on largest image dimension
            int fontSize = image.Width > image.Height ? image.Width / 12 : image.Height / 12;
            Font font = SystemFonts.CreateFont("Impact", fontSize);

            //compute text render size and font outline size
            SizeF botTextSize = TextMeasurer.Measure(bottomText, new RendererOptions(font));
            float outlineSize = fontSize / 15.0f;

            //determine top & bottom text location
            float padding = 10f;
            float textMaxWidth = image.Width - (padding * 2);
            PointF topLeftLocation = new PointF(padding, padding);
            PointF bottomLeftLocation = new PointF(padding, image.Height - botTextSize.Height - padding * 2);

            //white brush for text fill and black pen for text outline
            SolidBrush brush = new SolidBrush(Color.White);
            Pen pen = new Pen(Color.Black, outlineSize);

            TextGraphicsOptions options = new TextGraphicsOptions()
            {
                WrapTextWidth = textMaxWidth,
                HorizontalAlignment = HorizontalAlignment.Center,
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

        public static void ResizeProportional(this Image image, double scaleFactor)
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

        public static Image WatermarkImage(string baseImageFilename, string watermarkImageFilename, AnchorPositionMode anchorPos = AnchorPositionMode.Bottom, int paddingScale = 10, double watermarkScale = 0.2)
        {
            var baseImage = Image.Load(baseImageFilename);
            using (var watermarkImage = Image.Load(watermarkImageFilename))
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
                Point position;
                switch (anchorPos)
                {
                    case AnchorPositionMode.BottomRight:
                        position = new Point(baseImage.Width - watermarkImage.Width - paddingHorizontal, baseImage.Height - watermarkImage.Height - paddingVertical);
                        break;

                    case AnchorPositionMode.Bottom:
                        position = new Point(baseImage.Width / 2 - watermarkImage.Width / 2, baseImage.Height - watermarkImage.Height - paddingVertical);
                        break;

                    default:
                        position = new Point(0, 0);
                        break;

                }

                //draw the watermark on the base image with 80% opacity
                baseImage.Mutate(x => x.DrawImage(watermarkImage, position, 0.8f));

                return baseImage;
            }
        }

        public static Image ThiccImage(string imageFilename, int thiccCount)
        {
            var image = Image.Load(imageFilename);
            int originalWidth = image.Width;
            int originalHeight = image.Height;

            image.Mutate(x => x.Resize(thiccCount * originalWidth, image.Height));
            return image;
        }

        public static Image MosaicImage(string baseImageFilename, string tileImageFilename, double tileScaleFactor = 0.01, float opacity = 0.5f)
        {
            var outputImage = Image.Load<Rgba32>(baseImageFilename);
            using (var tileImage = Image.Load<Rgba32>(tileImageFilename))
            {
                tileImage.ResizeProportional(outputImage.Width / (double)tileImage.Width * tileScaleFactor);

                for (int y = 0; y < outputImage.Height; y += tileImage.Height)
                {
                    for (int x = 0; x < outputImage.Width; x += tileImage.Width)
                    {
                        Point tileUpperLeftLoc = new Point(x, y);
                        Rgba32 avgColor = GetAverageColor(outputImage, x, y, tileImage.Width, tileImage.Height);
                        outputImage.Mutate(i => i.Fill(avgColor, new Rectangle(x, y, tileImage.Width, tileImage.Height)));
                        outputImage.Mutate(i => i.DrawImage(tileImage, tileUpperLeftLoc, PixelColorBlendingMode.Normal, opacity));
                    }
                }
            }

            return outputImage;
        }

        public static Rgba32 GetAverageColor(Image<Rgba32> inputImage, int startX, int startY, int width, int height)
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

        public static Image<Rgba32> ProjectOnto(string projectImageFilename, string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                throw new ArgumentException("Could not find the given JSON file.");
            }

            //load and deserialize the file contents
            string jsonContents = File.ReadAllText(jsonPath);
            JObject projectInfo = JsonConvert.DeserializeObject<JObject>(jsonContents);

            //parse the image name and points
            string baseImageFilename = (string)projectInfo["baseImage"];

            Point topLeft = new Point((int)projectInfo["topLeft"]["x"], (int)projectInfo["topLeft"]["y"]);
            Point topRight = new Point((int)projectInfo["topRight"]["x"], (int)projectInfo["topRight"]["y"]);
            Point bottomLeft = new Point((int)projectInfo["bottomLeft"]["x"], (int)projectInfo["bottomLeft"]["y"]);
            Point bottomRight = new Point((int)projectInfo["bottomRight"]["x"], (int)projectInfo["bottomRight"]["y"]);

            return ProjectOnto(projectImageFilename, Path.Combine("assets", "images", baseImageFilename), topLeft, topRight, bottomLeft, bottomRight);
        }

        public static Image<Rgba32> ProjectOnto(string projectImageFilename, string baseImageFilename,
            Point topLeft,
            Point topRight,
            Point bottomLeft,
            Point bottomRight)
        {
            using (var projectImage = Image.Load(projectImageFilename))
            using (var baseImage = Image.Load(baseImageFilename))
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
                outputImage.Mutate(x => x.DrawImage(baseImage, new Point(0, 0), 1.0f));

                return outputImage;
            }
        }

        public static string ProjectText(string text, string baseImageFilename,
            Point topLeft,
            Point topRight,
            Point bottomLeft,
            Point bottomRight)
        {
            using (var baseImage = Image.Load(baseImageFilename))
            using (var textImage = new Image<Rgba32>(1920, 1080))
            using (var outputImage = new Image<Rgba32>(baseImage.Width, baseImage.Height))

            {
                int fontSize = textImage.Width / 10;
                Font font = SystemFonts.CreateFont("Impact", fontSize);

                //determine text location
                float padding = 10f;
                float textMaxWidth = textImage.Width - (padding * 2);
                PointF topLeftLocation = new PointF(padding, padding * 2);

                //black brush for text fill
                SolidBrush brush = new SolidBrush(Color.Black);

                //wrap and align text before drawing
                TextGraphicsOptions options = new TextGraphicsOptions()
                {
                    WrapTextWidth = textMaxWidth,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };

                //draw text on the text canvas
                textImage.Mutate(x => x.BackgroundColor(Color.White));
                textImage.Mutate(x => x.DrawText(options, text, font, brush, topLeftLocation));

                //compute the transformation matrix based on the destination points and apply it to the text image
                Matrix4x4 transformMat = TransformHelper.ComputeTransformMatrix(textImage.Width, textImage.Height, topLeft, topRight, bottomLeft, bottomRight);
                textImage.Mutate(x => x.Transform(new ProjectiveTransformBuilder().AppendMatrix(transformMat)));

                //draw the projected text and the base image on the output image
                outputImage.Mutate(x => x.DrawImage(textImage, new Point(0, 0), 1.0f));
                outputImage.Mutate(x => x.DrawImage(baseImage, 1.0f));

                string outputFilename = $"{Guid.NewGuid().ToString("N")}.jpg";
                outputImage.Save(outputFilename);

                return outputFilename;
            }
        }
    }
}
