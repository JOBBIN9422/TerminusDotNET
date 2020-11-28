using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using VideoLibrary;

namespace TerminusDotNetCore.Helpers
{
    public class LibVideoDownloader : IYoutubeDownloader
    {
        public async Task<string> DownloadYoutubeVideoAsync(string url)
        {
            //define the directory to save video files to
            string tempPath = Path.Combine(Environment.CurrentDirectory, "assets", "temp");
            string videoDataFullPath;

            //download the youtube video data (usually .mp4 or .webm)
            var youtube = YouTube.Default;
            var video = await youtube.GetVideoAsync(url);
            var videoData = await video.GetBytesAsync();

            //give the video file a unique name to prevent collisions
            //  **if libvideo fails to fetch the video's title, it names the file 'YouTube.mp4'**
            string videoDataFilename = $"{Guid.NewGuid().ToString("N")}{Path.GetExtension(video.FullName)}";

            //write the downloaded media file to the temp assets dir
            videoDataFullPath = Path.Combine(tempPath, videoDataFilename);
            await File.WriteAllBytesAsync(videoDataFullPath, videoData);

            return videoDataFullPath;
        }
    }
}
