using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace TerminusDotNetCore.Helpers
{
    public class YoutubeExplodeDownloader : IYoutubeDownloader
    {
        public async Task<string> DownloadYoutubeVideoAsync(string url)
        {
            YoutubeClient ytClient = new YoutubeClient();
            //get the stream & video info for the current video
            var streamManifest = await ytClient.Videos.Streams.GetManifestAsync(Regex.Match(url, @"(?<=v=)[\w-]+").Value);
            var streamInfo = streamManifest.GetAudioOnly().WithHighestBitrate();

            //download the current stream
            string videoDataFilename = Path.Combine(Path.Combine("assets", "temp"), $"{Guid.NewGuid().ToString("N")}.{streamInfo.Container}");
            await ytClient.Videos.Streams.DownloadAsync(streamInfo, videoDataFilename);

            return videoDataFilename;
        }
    }
}
