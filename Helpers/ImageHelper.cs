using System;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.Fonts;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SixLabors.ImageSharp.Drawing.Processing;

namespace TerminusDotNetCore.Helpers
{
    public enum AnchorType
    {
        BottomCenter,
        BotttomRight
    }

    public static class ImageHelper
    {
        /// <summary>
        /// Deep-fries an image.
        /// </summary>
        /// <param name="imageFilename">Path to the input image.</param>
        /// <param name="numPasses">Number of times to deep-fry.</param>
        /// <returns>The deep-fried image.</returns>
        public static Image DeepfryImage(string imageFilename, uint numPasses = 1)
        {
            using (var image = Image.Load(imageFilename))
            using (var tempStream = new MemoryStream())
            {
                image.SaveAsJpeg(tempStream);

                var tempJpg = Image.Load(tempStream.ToArray(), new JpegDecoder());
                for (uint i = 0; i < numPasses; i++)
                {
                    tempJpg = Image.Load(tempStream.ToArray(), new JpegDecoder());
                    tempJpg.Mutate(x => x.Saturate(4.0f)
                                               .Contrast(4.0f)
                                               .GaussianSharpen());

                    //try to compress the image based on its file-type
                    tempJpg.SaveAsJpeg(tempStream, new JpegEncoder()
                    {
                        Quality = 1,
                    });
                }

                return tempJpg;
            }
        }

        public static Image GrayscaleImage(string imageFilename)
        {
            Image image = Image.Load(imageFilename);
            image.Mutate(x => x.Grayscale());
            return image;
        }

        /// <summary>
        /// Captions an image with top text and bottom text (Impact font).
        /// </summary>
        /// <param name="imageFilename">Path to the input image.</param>
        /// <param name="topText">Top text to render.</param>
        /// <param name="bottomText">Bottom text to render.</param>
        /// <returns>The image with the given text drawn on it.</returns>
        public static Image MemeCaptionImage(string imageFilename, string topText, string bottomText)
        {
            Image image = Image.Load(imageFilename);
            //calculate font size based on largest image dimension
            int fontSize = image.Width > image.Height ? image.Width / 12 : image.Height / 12;
            Font font = SystemFonts.CreateFont("Impact", fontSize);

            //compute text render size and font outline size
            FontRectangle botTextSize = TextMeasurer.Measure(bottomText, new RendererOptions(font));
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
                TextOptions = new TextOptions()
                {
                    WrapTextWidth = textMaxWidth,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };

            //render text
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

        /// <summary>
        /// Scale an image by the given multiplier while maintaining its aspect ratio.
        /// </summary>
        /// <param name="image">Image to scale.</param>
        /// <param name="scaleFactor">Multiplier to scale the image by.</param>
        public static void ResizeProportional(this Image image, double scaleFactor)
        {
            double aspectRatio = image.Width / (double)image.Height;

            int resizeX;
            int resizeY;

            //compute new size based on whether or not the image is portrait or landscape 
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

        /// <summary>
        /// Draws a given image as an overlay on another image.
        /// </summary>
        /// <param name="baseImageFilename">Path to the base image.</param>
        /// <param name="watermarkImageFilename">Path to the image to be drawn on the base image.</param>
        /// <param name="anchorPos">Position to draw the watermark image at.</param>
        /// <param name="paddingScale">How much padding to use in positioning.</param>
        /// <param name="watermarkScale">How much to scale the watermark when drawn.</param>
        /// <returns>The given base image with the watermark drawn on it.</returns>
        public static Image WatermarkImage(string baseImageFilename, 
            string watermarkImageFilename, 
            AnchorPositionMode anchorPos = AnchorPositionMode.Bottom, 
            double paddingPercentageHorizontal = 0.10, 
            double paddingPercentageVertical = 0.1, 
            double watermarkScale = 0.2, 
            float opacity = 0.8f)
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
                int paddingHorizontal = (int)(baseImage.Width * paddingPercentageHorizontal);
                int paddingVertical = (int)(baseImage.Height * paddingPercentageVertical);

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

                    case AnchorPositionMode.BottomLeft:
                        position = new Point(paddingHorizontal, baseImage.Height - watermarkImage.Height - paddingVertical);
                        break;

                    default:
                        position = new Point(0, 0);
                        break;
                }

                //draw the watermark on the base image with 80% opacity (make this an argument?)
                baseImage.Mutate(x => x.DrawImage(watermarkImage, position, opacity));

                return baseImage;
            }
        }
        
        public static Image MirrorImage(string imageFilename, FlipMode flipMode, bool topAndOrLeftHalf = true)
        {
            var image = Image.Load(imageFilename);
            image.Mutate(x => x.AutoOrient());

            Rectangle cropRect;
            Point drawPoint;

            //mirror the image based on the given flip mode
            using (var mirrorHalf = image.Clone(x => x.Flip(flipMode)))
            {
                //define crop region and draw location based on given flip mode
                if (flipMode == FlipMode.Horizontal)
                {
                    if (topAndOrLeftHalf)
                    {
                        cropRect = new Rectangle(image.Width / 2, 0, image.Width / 2, image.Height);
                        drawPoint = new Point(image.Width / 2, 0);
                    }
                    else
                    {
                        cropRect = new Rectangle(0, 0, image.Width / 2, image.Height);
                        drawPoint = new Point(0, 0);
                    }
                }
                else
                {
                    if (topAndOrLeftHalf)
                    {
                        cropRect = new Rectangle(0, image.Height / 2, image.Width, image.Height / 2);
                        drawPoint = new Point(0, image.Height / 2);
                    }
                    else
                    {
                        cropRect = new Rectangle(0, 0, image.Width, image.Height / 2);
                        drawPoint = new Point(0, 0);
                    }
                }

                //crop the flipped image and draw it on the base image
                mirrorHalf.Mutate(x => x.Crop(cropRect));

                image.Mutate(x => x.DrawImage(mirrorHalf, drawPoint, 1.0f));
            }

            return image;
        }

        /// <summary>
        /// Stretches an image by the given multiplier.
        /// </summary>
        /// <param name="imageFilename">Path to input image.</param>
        /// <param name="thiccCount">Width multiplier.</param>
        /// <returns>The given image stretched by the input amount.</returns>
        public static Image ThiccImage(string imageFilename, int thiccCount)
        {
            var image = Image.Load(imageFilename);
            int originalWidth = image.Width;
            int originalHeight = image.Height;

            //scale the image's width by the given value
            image.Mutate(x => x.Resize(thiccCount * originalWidth, image.Height));
            return image;
        }

        /// <summary>
        /// Tiles a given image across a base image, creating a mosaic.
        /// </summary>
        /// <param name="baseImageFilename">Path to base input image.</param>
        /// <param name="tileImageFilename">Path to image to be used as a tile.</param>
        /// <param name="tileScaleFactor">Size of each tile.</param>
        /// <param name="opacity">Opacity of each tile.</param>
        /// <returns>The given base image recreated out of tiles.</returns>
        public static Image MosaicImage(string baseImageFilename, string tileImageFilename, double tileScaleFactor = 0.01, float opacity = 0.5f)
        {
            var outputImage = Image.Load<Rgba32>(baseImageFilename);
            using (var tileImage = Image.Load<Rgba32>(tileImageFilename))
            {
                //scale the tile image to its output size
                tileImage.ResizeProportional(outputImage.Width / (double)tileImage.Width * tileScaleFactor);

                for (int y = 0; y < outputImage.Height; y += tileImage.Height)
                {
                    for (int x = 0; x < outputImage.Width; x += tileImage.Width)
                    {
                        //define the draw point of a new tile 
                        Point tileUpperLeftLoc = new Point(x, y);

                        //compute the average color of the tile cell and draw it along with the tile
                        Rgba32 avgColor = GetAverageColor(outputImage, x, y, tileImage.Width, tileImage.Height);
                        outputImage.Mutate(i => i.Fill(avgColor, new Rectangle(x, y, tileImage.Width, tileImage.Height)));
                        outputImage.Mutate(i => i.DrawImage(tileImage, tileUpperLeftLoc, PixelColorBlendingMode.Normal, opacity));
                    }
                }
            }

            return outputImage;
        }

        /// <summary>
        /// Gets the average RGB value of a given rectangle.
        /// </summary>
        /// <param name="inputImage">Path to the input image.</param>
        /// <param name="startX">Rectangle x location.</param>
        /// <param name="startY">Rectangle y location.</param>
        /// <param name="width">Rectangle width.</param>
        /// <param name="height">Rectangle height.</param>
        /// <returns>The average color value (normalized RGBA) of the given region.</returns>
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

            //return normalized RGBA average
            Rgba32 avgColor = new Rgba32(
                    (float)Math.Sqrt(red / numPixels) / 255.0f,
                    (float)Math.Sqrt(green / numPixels) / 255.0f,
                    (float)Math.Sqrt(blue / numPixels) / 255.0f,
                1);
            return avgColor;
        }

        /// <summary>
        /// Projects the given image onto a base image.
        /// </summary>
        /// <param name="projectImageFilename">Path to image to project onto base image.</param>
        /// <param name="jsonPath">Path to JSON file containing information about the base image.</param>
        /// <returns>An image containing the given input projected onto the base image.</returns>
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

        /// <summary>
        /// Projects the given image onto a base image.
        /// </summary>
        /// <param name="projectImageFilename">Path to image to be projected.</param>
        /// <param name="baseImageFilename">Path to base image.</param>
        /// <param name="topLeft">Top-left corner of the projection region.</param>
        /// <param name="topRight">Top-right corner of the projection region.</param>
        /// <param name="bottomLeft">Bottom-left corner of the projection region.</param>
        /// <param name="bottomRight">Bottom-right corner of the projection region.</param>
        /// <returns>An image containing the given input projected onto the base image.</returns>
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

        /// <summary>
        /// Projects the given text onto a base image.
        /// </summary>
        /// <param name="text">The text to be projected.</param>
        /// <param name="jsonPath">Path to JSON file containing information about the base image.</param>
        /// <returns>The base image with the given text projected onto it.</returns>
        public static Image<Rgba32> ProjectText(string text, string jsonPath)
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

            return ProjectText(text, Path.Combine("assets", "images", baseImageFilename), topLeft, topRight, bottomLeft, bottomRight);
        }

        /// <summary>
        /// Projects the given text onto a base image.
        /// </summary>
        /// <param name="text">The text to be projected.</param>
        /// <param name="baseImageFilename">Path to base image.</param>
        /// <param name="topLeft">Top-left corner of the projection region.</param>
        /// <param name="topRight">Top-right corner of the projection region.</param>
        /// <param name="bottomLeft">Bottom-left corner of the projection region.</param>
        /// <param name="bottomRight">Bottom-right corner of the projection region.</param>
        /// <returns>The base image with the given text projected onto it.</returns>
        public static Image<Rgba32> ProjectText(string text, string baseImageFilename,
            Point topLeft,
            Point topRight,
            Point bottomLeft,
            Point bottomRight)
        {
            using (var baseImage = Image.Load(baseImageFilename))
            using (var textImage = new Image<Rgba32>(1920, 1080))
            {
                var outputImage = new Image<Rgba32>(baseImage.Width, baseImage.Height);

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
                    TextOptions = new TextOptions()
                    {
                        WrapTextWidth = textMaxWidth,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
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

                return outputImage;
            }
        }
    }
}
