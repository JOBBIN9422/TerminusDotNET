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

        private List<string> DownloadAttachments(IReadOnlyCollection<Attachment> attachments)
        {
            var webClient = new WebClient();
            var returnImgs = new List<string>();
            var imgIndex = 0;

            foreach (var attachment in attachments)
            {
                var filename = attachment.Filename;
                var url = attachment.Url;

                var downloadFilename = $"temp{imgIndex}{Path.GetExtension(filename)}";
                imgIndex++;
                webClient.DownloadFile(url, downloadFilename);
                //do some shit with the image

                //DeepfryImage(downloadFilename);

                returnImgs.Add(downloadFilename);
            }

            return returnImgs;
        }

        public List<string> DeepfryImages(IReadOnlyCollection<Attachment> attachments)
        {
            var images = DownloadAttachments(attachments);

            //download the image(s)
            foreach (var image in images)
            {
                DeepfryImage(image);
            }

            return images;
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

        public List<string> MemeCaptionImages(IReadOnlyCollection<Attachment> attachments, string topText, string bottomText)
        {
            var images = DownloadAttachments(attachments);

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
    }
}
