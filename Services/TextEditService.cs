using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using TerminusDotNetCore.Modules;

namespace TerminusDotNetCore.Services
{
    public class TextEditService : ICustomService
    {
        public IConfiguration Config { get; set; }

        public ServiceControlModule ParentModule { get; set; }

        private Dictionary<char, char> _wideTextMap = new Dictionary<char, char>();

        //unicode table values for full-width char mapping (punctuation, a/A - z/Z, 0-9)
        private readonly int _fullWidthOffset = 65248;
        private readonly int _unicodeStartIndex = 33;
        private readonly int _unicodeEndIndex = 127;

        public void Init()
        {
            //generate mapping from half-width to full-width chars (e.g. 'a' to 'ａ')
            for (int i = _unicodeStartIndex; i < _unicodeEndIndex; i++)
            {
                _wideTextMap.Add((char)i, (char)(i + _fullWidthOffset));
            }
        }

        public TextEditService()
        {
            Init();
        }

        public string ConvertToPlaintext(string message)
        {
            return $"`{message}`";
        }

        public string ConvertToFullWidth(string message)
        {
            message = message.Replace(" ", "  ");

            foreach (char c in message)
            {
                if (!char.IsWhiteSpace(c))
                {
                    try
                    {
                        message = message.Replace(c, _wideTextMap[c]);
                    }
                    catch (KeyNotFoundException)
                    {
                        continue;
                    }
                }
            }
            return message;
        }

        public string ConvertToMemeCase(string message)
        {
            string newMsg = "";
            for (int i = 0; i < message.Length; i++)
            {
                if (char.IsLetter(message[i]))
                {
                    if (i%2 == 1)
                    {
                        newMsg += char.ToUpper(message[i]);
                    }
                    else
                    {
                        newMsg += char.ToLower(message[i]);
                    }
                }
                else
                {
                    newMsg += message[i];
                }
            }
            return newMsg;
        }
    }
}
