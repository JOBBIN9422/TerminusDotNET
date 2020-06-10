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
        Audio,
        Plaintext
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

        private static readonly string[] _validPlaintextExtensions = {
            ".txt",
            ".py",
            ".cpp",
            ".hpp",
            ".h",
            ".c",
            ".java",
            ".cs"
        };

        public async static Task<IReadOnlyCollection<Attachment>> GetMostRecentAttachmentsAsync(SocketCommandContext context, AttachmentFilter filter, int priorMsgCount = 20)
        {
            //choose the array containing the proper file extensions to filter by 
            string[] validExtensions = GetValidExtensions(filter);

            var attachments = context.Message.Attachments;

            //if there are no attachments in the current message
            if (attachments == null || attachments.Count == 0)
            {
                //check the last 20 messages for attachments (from most recent to oldest)
                var messages = await context.Channel.GetMessagesAsync(priorMsgCount).FlattenAsync();
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
                return null;
            }
            else
            {
                //if there are valid attachments in the current message
                if (AttachmentsAreValid(attachments, validExtensions))
                {
                    return attachments;
                }
                else
                {
                    return null;
                }
            }
        }

        private static string[] GetValidExtensions(AttachmentFilter filter)
        {
            string[] validExtensions;

            switch (filter)
            {
                case AttachmentFilter.Audio:
                    validExtensions = _validAudioExtensions;
                    break;

                case AttachmentFilter.Images:
                    validExtensions = _validImageExtensions;
                    break;

                case AttachmentFilter.Plaintext:
                    validExtensions = _validPlaintextExtensions;
                    break;

                default:
                    validExtensions = new string[] { };
                    break;
            }

            return validExtensions;
        }

        //for use outside the class 
        public static bool AttachmentsAreValid(IReadOnlyCollection<IAttachment> attachments, AttachmentFilter filter)
        {
            string[] validExtensions = GetValidExtensions(filter);
            return AttachmentsAreValid(attachments, validExtensions);
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
            string extension = Path.GetExtension(filename).ToLower();
            return Array.Exists(validExtensions, element => element == extension);
        }
        
        public static List<string> DownloadAttachments(IMessage message)
        {
            if (message.Attachments.Count == 0)
            {
                throw new ArgumentException("The given message did not have any attachments.");
            }

            return DownloadAttachments(message.Attachments);
        }

        public static List<string> DownloadAttachments(IReadOnlyCollection<IAttachment> attachments)
        {
            using (var webClient = new WebClient())
            {
                var attachmentFiles = new List<string>();

                foreach (var attachment in attachments)
                {
                    var filename = attachment.Filename;
                    var url = attachment.Url;
                    var fileIdString = System.Guid.NewGuid().ToString("N");
                    
                    var downloadFilename = Path.Combine("assets", "temp", $"{fileIdString}{Path.GetExtension(filename)}");
                    webClient.DownloadFile(url, downloadFilename);

                    attachmentFiles.Add(downloadFilename);
                }
                return attachmentFiles;
            }
        }

        public static string DownloadPersistentAudioAttachment(IAttachment attachment)
        {
            using (var webClient = new WebClient())
            {

                var filename = attachment.Filename;
                var url = attachment.Url;
                var fileIdString = System.Guid.NewGuid().ToString("N");
                    
                var downloadFilename = Path.Combine("assets", "audio", filename);
                webClient.DownloadFile(url, downloadFilename);

                return Path.GetFileName(downloadFilename);
            }
        }

        public static List<string> GetTempAssets(string regex = "*")
        {
            DirectoryInfo d = new DirectoryInfo(Path.Combine("assets", "temp"));

            //Filter files by regex
            FileInfo[] Files = d.GetFiles(regex);

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
