using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Helpers
{
    public class PortraitAiClient
    {
        private static readonly string PORTRAITAI_BASE_ADDRESS = "https://a7.portrait-ai.com/";
        private static readonly string POST_IMAGE_ADDRESS = $"{PORTRAITAI_BASE_ADDRESS}v1/c/submit-user-image.php";

        private static HttpClient _client = new HttpClient();

        public static async Task PostImage(string imageFilename)
        {
            string mimeType;

            if (!new FileExtensionContentTypeProvider().TryGetContentType(Path.GetFileName(imageFilename), out mimeType))
            {
                mimeType = "application/octet-stream";
            }

            string boundary = $"----------{Guid.NewGuid().ToString("N")}";
            MultipartFormDataContent imageContent = new MultipartFormDataContent(boundary);

            ByteArrayContent imageDataContent = new ByteArrayContent(File.ReadAllBytes(imageFilename));
            imageDataContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
            imageContent.Add(imageDataContent, "image", Path.GetFileName(imageFilename));

            HttpResponseMessage httpResponse = await _client.PostAsync(POST_IMAGE_ADDRESS, imageContent);
            Console.WriteLine(await httpResponse.Content.ReadAsStringAsync());
        }
    }
}
