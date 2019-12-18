using System;
using System.Collections.Generic;
using System.Text;
using TerminusDotNetCore.Modules;

namespace TerminusDotNetCore.Services
{
    public class WideTextService : ICustomService
    {
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

        public WideTextService()
        {
            Init();
        }

        public string ConvertMessage(string message)
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
    }
}
