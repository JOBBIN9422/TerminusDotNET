using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace TerminusDotNetCore.Helpers
{
    public enum AttachmentFilter
    {
        Images,
        Audio
    }
    
    public static class AttachmentHelper
    {
        private static readonly string[] _validImageExtensions = {
            ".jpg",
            ".jpeg",
            ".png",
            ".bmp",
            ".gif"
        };

        private static readonly string[] _validAudioExtensions = {
            ".mp3"
        };

        public async static Task<IReadOnlyCollection<Attachment>> GetAttachmentsAsync(SocketCommandContext context, AttachmentFilter filter)
        {
            //choose the array containing the proper file extensions to filter by 
            string[] validExtensions = new string[] { };
            switch (filter)
            {
                case AttachmentFilter.Audio:
                    validExtensions = _validAudioExtensions;
                    break;

                case AttachmentFilter.Images:
                    validExtensions = _validImageExtensions;
                    break;

                default:
                    validExtensions = _validImageExtensions;
                    break;
            }

            var attachments = context.Message.Attachments;
            if (attachments == null || attachments.Count == 0)
            {
                //check the last 20 messages for attachments (from most recent to oldest)
                var messages = await context.Channel.GetMessagesAsync(20).FlattenAsync();
                foreach (var message in messages)
                {
                    if (message.Attachments.Count > 0)
                    {
                        //skip this message if its attachments don't pass the filter
                        if (!AttachmentsAreValid(message.Attachments, validExtensions))
                        {
                            continue;
                        }

                        //return attachments if they're all valid
                        else
                        {
                            return (IReadOnlyCollection<Attachment>)message.Attachments;
                        }
                    }
                }

                //if none of the previous messages had any attachments
                throw new NullReferenceException("No attachments were found in the current or previous messages.");
            }
            else
            {
                if (AttachmentsAreValid(attachments, validExtensions))
                {
                    return attachments;
                }
                else
                {
                    throw new NullReferenceException("No attachments were found in the current or previous messages.");
                }
            }
        }

        private static bool AttachmentsAreValid(IReadOnlyCollection<IAttachment> attachments, string[] validExtensions)
        {
            foreach (var attachment in attachments)
            {
                if (!FileIsValid(attachment.Filename, validExtensions))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool FileIsValid(string filename, string[] validExtensions)
        {
            string extension = Path.GetExtension(filename);
            return Array.Exists(validExtensions, element => element == extension);
        }
        
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

        public static List<string> GetTempAssets(string regex = "*")
        {
            DirectoryInfo d = new DirectoryInfo(@"assets/temp");//Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles(regex); //Getting files based on regex params
            List<string> filePaths = new List<string>();
            foreach(FileInfo file in Files )
            {
              filePaths.Add(file.FullName);
            }
            return filePaths;
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
