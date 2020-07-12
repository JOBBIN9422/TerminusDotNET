using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace TerminusDotNetCore.Helpers
{
    public class PortraitAiClient
    {
        private static HttpClient _client = new HttpClient();
        private static Random _random = new Random();

        private static readonly int MAX_RETRY_COUNT = 20;

        private static string STYLES_LIST = "[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19]";
        private static string HASH_SUBSTITUTE_PLACEHOLDER = "HASH";
        private static string STYLE_INDEX_PLACEHOLDER = "STYLE";
        private static string PORTRAITAI_BASE_ADDRESS = $"https://a{_random.Next(1, 16)}.portrait-ai.com/";
        private static string POST_IMAGE_ADDRESS = $"{PORTRAITAI_BASE_ADDRESS}v1/c/submit-user-image.php";
        private static string MAKE_STYLES_ADDRESS = $"{PORTRAITAI_BASE_ADDRESS}v1/c/make-styles.php";

        private static string STYLES_READY_ADDRESS = $"{PORTRAITAI_BASE_ADDRESS}v1/c/styles-ready.php?hex={HASH_SUBSTITUTE_PLACEHOLDER}";
        private static string GET_PORTRAIT_IMAGE_ADDRESS = $"{PORTRAITAI_BASE_ADDRESS}v1/cropped/{HASH_SUBSTITUTE_PLACEHOLDER}/portraitai.com-{STYLE_INDEX_PLACEHOLDER}.jpg";

        private static void ReseedUrls()
        {
            PORTRAITAI_BASE_ADDRESS = $"https://a{_random.Next(1, 16)}.portrait-ai.com/";
            POST_IMAGE_ADDRESS = $"{PORTRAITAI_BASE_ADDRESS}v1/c/submit-user-image.php";
            MAKE_STYLES_ADDRESS = $"{PORTRAITAI_BASE_ADDRESS}v1/c/make-styles.php";

            STYLES_READY_ADDRESS = $"{PORTRAITAI_BASE_ADDRESS}v1/c/styles-ready.php?hex={HASH_SUBSTITUTE_PLACEHOLDER}";
            GET_PORTRAIT_IMAGE_ADDRESS = $"{PORTRAITAI_BASE_ADDRESS}v1/cropped/{HASH_SUBSTITUTE_PLACEHOLDER}/portraitai.com-{STYLE_INDEX_PLACEHOLDER}.jpg";
        }

        public static async Task PostImage(string imageFilename)
        {
            //choose a random subdomain (the site does this internally)
            ReseedUrls();

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

            //send the image to be cropped and extract the crop hash from the response
            HttpResponseMessage postImageResponse = await _client.PostAsync(POST_IMAGE_ADDRESS, imageContent);
            JObject postImageResponseContent = JsonConvert.DeserializeObject<JObject>(await postImageResponse.Content.ReadAsStringAsync());
            string cropHash = postImageResponseContent["crop_hashes"][0].ToString();

            //build the content for the make-styles request
            MultipartFormDataContent makeStylesContent = new MultipartFormDataContent(boundary);

            //add the crop hash
            StringContent hexContent = new StringContent(cropHash);
            hexContent.Headers.Add("Content-Disposition", "form-data; name=\"hex\"");
            makeStylesContent.Add(hexContent);

            //add the possible styles list
            StringContent stylesContent = new StringContent(STYLES_LIST);
            stylesContent.Headers.Add("Content-Disposition", "form-data; name=\"styles\"");
            makeStylesContent.Add(stylesContent);

            //send the make-styles request in order to get available portrait styles to choose from
            HttpResponseMessage makeStylesResponse = await _client.PostAsync(MAKE_STYLES_ADDRESS, makeStylesContent);

            JObject stylesTable = new JObject();
            int requestCount = 0;
            
            //keep requesting available styles until we get a non-empty response
            do
            {
                HttpResponseMessage stylesReadyResponse = await _client.GetAsync(STYLES_READY_ADDRESS.Replace(HASH_SUBSTITUTE_PLACEHOLDER, cropHash));
                requestCount++;
                string responseContent = await stylesReadyResponse.Content.ReadAsStringAsync();
                Thread.Sleep(1000);

                //ignore JSON array responses - almost always empty
                if (responseContent.StartsWith('['))
                {
                    continue;
                }
                stylesTable = JsonConvert.DeserializeObject<JObject>(responseContent);
            } while (stylesTable.Count == 0 && requestCount < MAX_RETRY_COUNT);

            //if we didn't get anything after waiting, something is wrong - can't get portraits
            if (stylesTable.Count == 0)
            {
                throw new TimeoutException($"Could not get a portrait from {PORTRAITAI_BASE_ADDRESS} (rate-limited or server-side issue).");
            }

            //build a list of style numbers and choose one at random
            List<int> styleNums = new List<int>();
            foreach (var entry in stylesTable)
            {
                styleNums.Add(entry.Value.ToObject<int>());
            }
            int styleNum = styleNums[_random.Next(0, stylesTable.Count)];

            //get the portrait generated for the currently chosen style number
            HttpResponseMessage getPortraitResponse = await _client.GetAsync(GET_PORTRAIT_IMAGE_ADDRESS
                .Replace(HASH_SUBSTITUTE_PLACEHOLDER, cropHash)
                .Replace(STYLE_INDEX_PLACEHOLDER, styleNum.ToString()));

            //overwrite the given image with the data from the response
            byte[] portraitImageData = await getPortraitResponse.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(imageFilename, portraitImageData);
        }
    }
}
