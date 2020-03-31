using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Web;
using System.IO;

namespace Terminus.Helpers
{
    class StonksHelper
    {
        public static async Task DownloadImage(string stock_name)
        {
            var httpClient = new HttpClient();
            var url = "http://68.201.65.4:1234/get_stock?ticker=" + stock_name;
            byte[] imageBytes = await httpClient.GetByteArrayAsync(url);

            string documentsPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);

            string localFilename = "graph.png";
            string localPath = Path.Combine("assets", "images", localFilename);
            File.WriteAllBytes(localPath, imageBytes);
        }
    }
}