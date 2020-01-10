using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using SixLabors.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.Fonts;
using System.Drawing;
using System.Numerics;

namespace TerminusDotNetCore.Helpers
{
    public class ImageHelper
    {
        public static System.Drawing.Color GetAverageColor(System.Drawing.Bitmap inputImage, int startX, int startY, int width, int height)
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

        public static void BlendImage(Bitmap image, System.Drawing.Color blendColor, double amount)
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

        public static System.Drawing.Color BlendColor(System.Drawing.Color baseColor, System.Drawing.Color blendColor, double amount)
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
