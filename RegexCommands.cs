using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TerminusDotNetCore
{
    public class RegexCommands
    {
        private Dictionary<string, string> _regexToMsgMap;

        public RegexCommands()
        {
            _regexToMsgMap = new Dictionary<string, string>();

            //init regex-message map from file
            using (StreamReader reader = new StreamReader(Path.Combine("assets","regex.txt")))
            {
                var currLine = string.Empty;
                while ((currLine = reader.ReadLine()) != null)
                {
                    var currLineTokens = currLine.Split('|');
                    _regexToMsgMap.Add(currLineTokens[0], currLineTokens[1]);
                }
            }
        }

        /// <summary>
        /// Run the input message across all regexes in the regex file.
        /// </summary>
        /// <param name="message">The input message to parse.</param>
        /// <returns>A list containing the messages for each matched regex.</returns>
        public List<string> ParseMessage(string message)
        {
            List<string> returnMessages = new List<string>();

            foreach (var regexString in _regexToMsgMap.Keys)
            {
                Match match = Regex.Match(message, regexString, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    String returnString = _regexToMsgMap[regexString];
                    for( int i = 1; match.Groups.Count >= i; i++ )
                    {
                        Console.WriteLine(match.Groups[i].Value);
                        if( returnString.Contains("%s") )
                        {
                            returnString = returnString.Replace("%s",match.Groups[i].Value);
                        }
                    }
                    returnMessages.Add(returnString);
                }
            }

            return returnMessages;
        }
    }
}
