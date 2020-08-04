using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Web;
using System.IO;
using System.Collections.Specialized;

namespace TerminusDotNetCore.Helpers
{
    class StonksHelper
    {
        public static async Task<string> DownloadImage(string baseIP, string stockName)
        {
            //init client/url (load IP from secrets.config)
            HttpClient httpClient = new HttpClient();
            string url = $"http://{baseIP}/get_stock?ticker=" + stockName;

            //check for additional user-entered query parameters (certified coner moment)
            string queryString = url.Substring(url.IndexOf('?') + 1);
            NameValueCollection queryParams = HttpUtility.ParseQueryString(queryString);
            if (queryParams.Count > 1)
            {
                throw new ArgumentException($"Blocked attempt to pass multiple query parameters: `?{queryString}`");
            }
            else
            {
                //download stock chart img for valid requests
                byte[] imageBytes = await httpClient.GetByteArrayAsync(url);

                string localFilename = "graph.png";
                string localPath = Path.Combine("assets", "images", localFilename);
                File.WriteAllBytes(localPath, imageBytes);

                return localPath;
            }
        }
    }
}