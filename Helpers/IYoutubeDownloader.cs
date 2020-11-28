using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Helpers
{
    interface IYoutubeDownloader
    {
        //download a video by its URL. Return the path to the downloaded file.
        public Task<string> DownloadYoutubeVideoAsync(string url);
    }
}
