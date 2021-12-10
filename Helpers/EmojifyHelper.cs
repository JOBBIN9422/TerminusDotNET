using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Helpers
{
    public class EmojifyHelper
    {
        public string EmojiDataPath { get; } = Path.Combine("assets", "emoji.json");
        public Dictionary<string, Dictionary<string, int>> EmojiHistograms { get; private set; }  = new Dictionary<string, Dictionary<string, int>>();

        private Random _random = new Random();

        public EmojifyHelper()
        {
            JObject emojiData = JObject.Parse(File.ReadAllText(EmojiDataPath));

            //iterate each word and extract its emoji frequencies
            foreach (KeyValuePair<string, JToken> currWordData in emojiData)
            {
                Dictionary<string, int> currWordEmojiHistogram = new Dictionary<string, int>();
                string currWord = currWordData.Key;
                JObject currWordEmojiFrequencies = (JObject)currWordData.Value;

                //iterate the emoji data for the current word and populate the histogram for that word
                foreach (KeyValuePair<string, JToken> currEmojiData in currWordEmojiFrequencies)
                {
                    string currEmoji = currEmojiData.Key;
                    int currEmojiFrequency = currEmojiData.Value.Value<int>();
                    currWordEmojiHistogram.Add(currEmoji, currEmojiFrequency);
                }
                
                //add the histogram to the map keyed by the current word
                EmojiHistograms.Add(currWord, currWordEmojiHistogram);
            }
        }

        //get an emoji for the current word by taking a weighted random sample from the values in its histogram
        //if no histogram exists for the given word, return null
        public string GetEmojiWeighted(string word)
        {
            if (!EmojiHistograms.ContainsKey(word))
            {
                return null;
            }

            string emoji = string.Empty;
            Dictionary<string, int> currHistogram = EmojiHistograms[word];
            int totalWeight = currHistogram.Sum(x => x.Value);

            int randSample = _random.Next(totalWeight);

            foreach (KeyValuePair<string, int> emojiData in currHistogram)
            {
                int currWordCount = emojiData.Value;
                if (randSample >= currWordCount)
                {
                    randSample -= currWordCount;
                }
                else
                {
                    emoji = emojiData.Key;
                    break;
                }
            }

            return emoji;
        }
    }
}
