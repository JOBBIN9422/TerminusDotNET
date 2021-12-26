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
        All,
        Images,
        Audio,
        Media,
        Plaintext
    }
    
    public static class AttachmentHelper
    {
        private static readonly string[] _wildcardExtension = {
            "*"
        };
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

        private static readonly string[] _validMediaExtensions = {
            ".mp3",
            ".mp4",
            ".wav",
            ".webm"
        };

        //any plaintext content (including source code files)
        private static readonly string[] _validPlaintextExtensions = {
            ".txt",
            ".py",
            ".cpp",
            ".hpp",
            ".h",
            ".c",
            ".java",
            ".cs",
            ".js"
        };

        //get the most recently posted attachments for the given context, or null if none exist
        public async static Task<IReadOnlyCollection<Attachment>> GetMostRecentAttachmentsAsync(IInteractionContext context, AttachmentFilter filter, int priorMsgCount = 20)
        {
            //filter attachments by file category
            string[] validExtensions = GetValidExtensions(filter);
            var recentMsgs = context.Channel.GetMessagesAsync(limit: priorMsgCount).Flatten();
            await foreach (var message in recentMsgs)
            {
                if (message.Attachments.Count > 0)
                {
                    //if we've found valid attachments
                    if (AttachmentsAreValid(message.Attachments, validExtensions))
                    {
                        return (IReadOnlyCollection<Attachment>)message.Attachments;
                    }
                }
            }

            return null;
        }

        //get a list of valid extensions for the given file category
        private static string[] GetValidExtensions(AttachmentFilter filter)
        {
            string[] validExtensions;

            switch (filter)
            {
                case AttachmentFilter.Audio:
                    validExtensions = _validAudioExtensions;
                    break;

                case AttachmentFilter.Media:
                    validExtensions = _validMediaExtensions;
                    break;

                case AttachmentFilter.Images:
                    validExtensions = _validImageExtensions;
                    break;

                case AttachmentFilter.Plaintext:
                    validExtensions = _validPlaintextExtensions;
                    break;

                case AttachmentFilter.All:
                    validExtensions = _wildcardExtension;
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
            //check each attachment's file extension against the given list of valid extensions
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
            //wildcards always valid
            if (Array.Exists(validExtensions, element => element == "*"))
            {
                return true;
            }

            //if not wildcard, check if the file's extension is in the extension filter array
            string extension = Path.GetExtension(filename).ToLower();
            return Array.Exists(validExtensions, element => element == extension);
        }

        public static List<string> DownloadAttachments(IReadOnlyCollection<IAttachment> attachments)
        {
            using (var webClient = new WebClient())
            {
                var attachmentFiles = new List<string>();

                //download each attachment file to the temp dir via webclient
                foreach (var attachment in attachments)
                {
                    var filename = attachment.Filename;
                    var url = attachment.Url;

                    //give each file a unique name to prevent overwriting 
                    var fileIdString = Guid.NewGuid().ToString("N");

                    //preserve file's extension in the full name
                    var downloadFilename = Path.Combine("assets", "temp", $"{fileIdString}{Path.GetExtension(filename)}");
                    webClient.DownloadFile(url, downloadFilename);

                    attachmentFiles.Add(downloadFilename);
                }
                return attachmentFiles;
            }
        }

        //download the attached audio file and save it locally for later use
        public static string DownloadPersistentAudioAttachment(IAttachment attachment)
        {
            using (var webClient = new WebClient())
            {
                string downloadFilename = Path.Combine("assets", "audio", attachment.Filename);
                webClient.DownloadFile(attachment.Url, downloadFilename);
                return Path.GetFileName(downloadFilename);
            }
        }

        public static List<string> GetTempAssets(string regex = "*")
        {
            DirectoryInfo d = new DirectoryInfo(Path.Combine("assets", "temp"));

            //Filter files by regex
            FileInfo[] Files = d.GetFiles(regex);

            List<string> filePaths = new List<string>();
            foreach(FileInfo file in Files)
            {
                filePaths.Add(file.FullName);
            }

            return filePaths;
        }

        public static List<string> GetTempAssets(AttachmentFilter filter)
        {
            string[] validExtensions = GetValidExtensions(filter);

            DirectoryInfo d = new DirectoryInfo(Path.Combine("assets", "temp"));

            //Filter files by regex
            FileInfo[] Files = d.GetFiles();

            List<string> filePaths = new List<string>();
            foreach (FileInfo file in Files)
            {
                if (FileIsValid(file.FullName, validExtensions))
                {
                    filePaths.Add(file.FullName);
                }
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

        //delete the file if it exists and return a success flag
        public static bool DeleteFile(string filename)
        {
            string filePath = Path.Combine("assets", "temp", filename);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
    }
}
