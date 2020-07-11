﻿using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TerminusDotNetCore.Helpers
{
    public class PortraitAiClient
    {
        private static readonly string PORTRAITAI_BASE_ADDRESS = "https://a7.portrait-ai.com/";
        private static readonly string POST_IMAGE_ADDRESS = $"{PORTRAITAI_BASE_ADDRESS}v1/c/submit-user-image.php";
        private static readonly string MAKE_STYLES_ADDRESS = $"{PORTRAITAI_BASE_ADDRESS}v1/c/make-styles.php";
        private static readonly string HASH_SUBSTITUTE_PLACEHOLDER = "HASH";
        private static readonly string STYLES_READY_ADDRESS = $"{PORTRAITAI_BASE_ADDRESS}v1/c/styles-ready.php?hex={HASH_SUBSTITUTE_PLACEHOLDER}";
        private static readonly string GET_PORTRAIT_IMAGE_ADDRESS = $"{PORTRAITAI_BASE_ADDRESS}v1/cropped/{HASH_SUBSTITUTE_PLACEHOLDER}/original.jpg";

        private static HttpClient _client = new HttpClient();

        public static async Task PostImage(string imageFilename)
        {
            //determine type of the image to be sent
            string mimeType;
            if (!new FileExtensionContentTypeProvider().TryGetContentType(Path.GetFileName(imageFilename), out mimeType))
            {
                mimeType = "application/octet-stream";
            }

            //create multi-part form content w/ boundary
            string boundary = $"----------{Guid.NewGuid().ToString("N")}";
            MultipartFormDataContent imageContent = new MultipartFormDataContent(boundary);

            //add the image data to the form
            ByteArrayContent imageDataContent = new ByteArrayContent(File.ReadAllBytes(imageFilename));
            imageDataContent.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType);
            imageContent.Add(imageDataContent, "image", Path.GetFileName(imageFilename));

            //extract the crop hash from the response
            HttpResponseMessage postImageResponse = await _client.PostAsync(POST_IMAGE_ADDRESS, imageContent);
            JObject postImageResponseContent = JsonConvert.DeserializeObject<JObject>(await postImageResponse.Content.ReadAsStringAsync());
            string cropHash = postImageResponseContent["crop_hashes"][0].ToString();

            //build the content for the make-styles request
            MultipartFormDataContent makeStylesContent = new MultipartFormDataContent(boundary);
            StringContent hexContent = new StringContent(cropHash);
            hexContent.Headers.Add("Content-Disposition", "form-data; name=\"hex\"");
            makeStylesContent.Add(hexContent);
            StringContent stylesContent = new StringContent("[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19]");
            stylesContent.Headers.Add("Content-Disposition", "form-data; name=\"styles\"");
            makeStylesContent.Add(stylesContent);

            //send the make-styles request
            HttpResponseMessage makeStylesResponse = await _client.PostAsync(MAKE_STYLES_ADDRESS, makeStylesContent);

            //get styles which are ready for download
            HttpResponseMessage stylesReadyResponse = await _client.GetAsync(STYLES_READY_ADDRESS);
            JArray stylesTable = JsonConvert.DeserializeObject<JArray>(await stylesReadyResponse.Content.ReadAsStringAsync());

            ////send the crop hash to get the portrait from the site
            //HttpResponseMessage getPortraitResponse = await _client.GetAsync(GET_PORTRAIT_IMAGE_ADDRESS.Replace(HASH_SUBSTITUTE_PLACEHOLDER, cropHash));

            ////overwrite the given image with the data from the response
            //byte[] portraitImageData = await getPortraitResponse.Content.ReadAsByteArrayAsync();
            //await File.WriteAllBytesAsync(imageFilename, portraitImageData);
        }
    }
}
