﻿using Discord;
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
        public async static Task<IReadOnlyCollection<Attachment>> GetMostRecentAttachmentsAsync(SocketCommandContext context, AttachmentFilter filter, int priorMsgCount = 20)
        {
            //filter attachments by file category
            string[] validExtensions = GetValidExtensions(filter);
            var attachments = context.Message.Attachments;

            if (attachments == null || attachments.Count == 0)
            {
                //look backwords by priorMsgCount messages for the most recent attachments
                var messages = await context.Channel.GetMessagesAsync(priorMsgCount).FlattenAsync();
                foreach (var message in messages)
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

                //if none of the previous messages had any filtered attachments
                return null;
            }
            //if there are valid attachments in the current message
            else
            {
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

        //get a list of valid extensions for the given file category
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
            //check if the file's extension is in the extension filter array
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

        public static string DownloadPersistentAudioAttachment(IAttachment attachment)
        {
            using (var webClient = new WebClient())
            {

                var filename = attachment.Filename;
                var url = attachment.Url;
                var fileIdString = Guid.NewGuid().ToString("N");
                    
                var downloadFilename = Path.Combine("assets", "audio", filename);
                webClient.DownloadFile(url, downloadFilename);

                return Path.GetFileName(downloadFilename);
            }
        }

        public static List<string> GetTempAssets(string regex = "*", bool fullPath = true)
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

        public static void DeleteFiles(List<string> files)
        {
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
    }
}
