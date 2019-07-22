using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord;
using TerminusDotNetConsoleApp.Modules;
using System.Drawing;
using ImageProcessor.Imaging.Formats;
using ImageProcessor;
using ImageProcessor.Imaging;

namespace TerminusDotNetConsoleApp.Services
{
    public class ImageService : ICustomService
    {
        public IServiceModule ParentModule { get; set; }

        public async Task ServiceReplyAsync(string s, EmbedBuilder embedBuilder = null)
        {
            if (embedBuilder == null)
            {
                await ParentModule.ServiceReplyAsync(s);
            }
            else
            {
                await ParentModule.ServiceReplyAsync(s, embedBuilder);
            }
        }

        //public async Task DeepfryImages(IReadOnlyCollection<Attachment> attachments)
        //{
        //    WebClient webClient = new WebClient();
        //    //download the image(s)
        //    foreach (var attachment in attachments)
        //    {
        //        var filename = attachment.Filename;
        //        var url = attachment.Url;

        //        var deepfryFilename = "temp" + Path.GetExtension(filename);
        //        webClient.DownloadFile(url, deepfryFilename);
        //        //do some shit with the image

        //        var embedBuilder = new EmbedBuilder()
        //        {
        //            Title = "dab",
        //            ImageUrl = $"attachment://{deepfryFilename}"
        //        };

        //        var embed = embedBuilder.Build();

        //        await ParentModule.ServiceReplyAsync("deep fried", embedBuilder);
        //    }
        //}

        public List<string> DeepfryImages(IReadOnlyCollection<Attachment> attachments)
        {
            WebClient webClient = new WebClient();
            List<string> returnImgs = new List<string>();

            //download the image(s)
            int imgIndex = 0;
            foreach (var attachment in attachments)
            {
                var filename = attachment.Filename;
                var url = attachment.Url;

                var deepfryFilename = $"temp{imgIndex}{Path.GetExtension(filename)}";
                imgIndex++;
                webClient.DownloadFile(url, deepfryFilename);
                //do some shit with the image

                DeepfryImage(deepfryFilename);

                returnImgs.Add(deepfryFilename);
            }

            return returnImgs;
        }

        public void DeleteImages(List<string> images)
        {
            foreach (var image in images)
            {
                File.Delete(image);
            }
        }

        private void DeepfryImage(string imageFilename)
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

        private void MemeCaptionImage(string imageFilename, string topText, string bottomText)
        {
            var imageBytes = File.ReadAllBytes(imageFilename);
            var format = new JpegFormat { Quality = 10 };

            int imageWidth = 0;
            int imageHeight = 0;
            using (var imageBmp = new Bitmap(imageFilename))
            {
                imageWidth = imageBmp.Width;
                imageHeight = imageBmp.Height;
            }

            var topTextLayer = new TextLayer()
            {
                FontFamily = new FontFamily("Arial"),
                Position = new Point(imageWidth / 4, imageHeight / 10),
                FontSize = 32,
                FontColor = System.Drawing.Color.White
            };

            var bottomTextLayer = new TextLayer()
            {
                FontFamily = new FontFamily("Arial"),
                Position = new Point(imageWidth * 3 / 4, imageHeight * 9 / 10),
                FontSize = 32,
                FontColor = System.Drawing.Color.White
            };

            using (var inStream = new MemoryStream(imageBytes))
            using (var outStream = new MemoryStream())
            using (var saveFileStream = new FileStream(imageFilename, FileMode.Open, FileAccess.Write))
            using (var imageFactory = new ImageFactory(preserveExifData: true))
            {
                imageFactory.Load(inStream)
                            .Watermark(topTextLayer)
                            .Watermark(bottomTextLayer);
                outStream.CopyTo(saveFileStream);
            }
        }
    }
}
