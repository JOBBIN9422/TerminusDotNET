﻿using Discord;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace TerminusDotNetCore.Helpers
{
    public static class AttachmentHelper
    {
        public static List<string> DownloadAttachments(IReadOnlyCollection<Attachment> attachments, string downloadPath = "")
        {
            using (var webClient = new WebClient())
            {
                var returnImgs = new List<string>();

                foreach (var attachment in attachments)
                {
                    var filename = attachment.Filename;
                    var url = attachment.Url;
                    var fileIdString = System.Guid.NewGuid().ToString("N");
                    
                    var downloadFilename = Path.Combine(downloadPath, $"{fileIdString}{Path.GetExtension(filename)}");
                    webClient.DownloadFile(url, downloadFilename);

                    returnImgs.Add(downloadFilename);
                }
                return returnImgs;
            }
        }

        public static void DeleteFiles(List<string> files)
        {
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
    }
}
