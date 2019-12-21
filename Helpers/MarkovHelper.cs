using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace TerminusDotNetCore.Helpers
{
    public class MarkovHelper
    {
        private Dictionary<string, Dictionary<string, int>> _markovCorpus = new Dictionary<string, Dictionary<string, int>>();
        private Random _random = new Random();

        public int KeyCount
        {
            get
            {
                return _markovCorpus.Keys.Count;
            }
        }

        public int TokenCount { get; private set; }

        public MarkovHelper(string filePath, params string[] preprocessChars)
        {
            
            using (StreamReader reader = new StreamReader(filePath))
            {
                //read a line from the sentence file
                string currSentence;
                while ((currSentence = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(currSentence))
                    {
                        continue;
                    }

                    AddSentenceToCorpus(currSentence, preprocessChars);
                }
            }
        }

        public MarkovHelper(List<string> sentences, params string[] preprocessChars)
        {
            foreach (string sentence in sentences)
            {
                //ignore commands
                if (!Regex.IsMatch(sentence, @"\!\w"))
                {
                    //strip emojis
                    AddSentenceToCorpus(Regex.Replace(sentence, @"\<:\w+:\d+\>", ""), preprocessChars);
                }
            }
        }

        private void AddSentenceToCorpus(string sentence, string[] preprocessChars = null)
        {
            //ignore empty sentences
            if (string.IsNullOrEmpty(sentence))
            {
                return;
            }

            if (preprocessChars != null)
            {
                //preprocess sentence
                foreach (string removeStr in preprocessChars)
                {
                    sentence = sentence.Replace(removeStr, "");
                }
            }

            //add start and end marker-keywords
            sentence = $"*START* {sentence} *END*";

            //tokenize by spaces
            string[] tokens = sentence.Split(' ');
            TokenCount += tokens.Length;

            //add any potential keys to the corpus
            foreach (string token in tokens)
            {
                if (!_markovCorpus.ContainsKey(token))
                {
                    _markovCorpus.Add(token, new Dictionary<string, int>());
                }
            }

            //slide the "window" across the current sentence and build word relationships
            for (int i = 0; i < tokens.Length - 1; i++)
            {
                //get the current key and the word following it
                string currKey = tokens[i];
                string currNextWord = tokens[i + 1];

                //count the number of times the current word has appeared after the current keyword
                Dictionary<string, int> currHistogram = _markovCorpus[currKey];
                if (!currHistogram.ContainsKey(currNextWord))
                {
                    currHistogram.Add(currNextWord, 1);
                }
                else
                {
                    currHistogram[currNextWord] += 1;
                }
            }
        }

        public string GenerateSentence()
        {
            string startWord = "";

            //pick a valid starting word at random
            //List<string> keyList = _markovCorpus.Keys.ToList();
            //do
            //{
            //    int i = _random.Next(keyList.Count);
            //    startWord = keyList[i];
            //} while (startWord == "*END*");
            List<string> possibleStartWords = _markovCorpus["*START*"].Keys.ToList();
            startWord = possibleStartWords[_random.Next(possibleStartWords.Count)];

            //generate the sentence
            string sentence = startWord;
            string currWord = startWord;

            do
            {
                currWord = GetWeightedNextWord(currWord);
                if (currWord != "*END*")
                {
                    sentence = $"{sentence} {currWord}";
                }
            } while (currWord != "*END*");

            return sentence;
        }

        public string GetWeightedNextWord(string word)
        {
            if (!_markovCorpus.ContainsKey(word))
            {
                throw new ArgumentException("The Markov corpus does not contain the given word.");
            }

            string result = "";
            Dictionary<string, int> currHistogram = _markovCorpus[word];
            int totalWeight = currHistogram.Sum(x => x.Value);

            int randSample = _random.Next(totalWeight);

            foreach (KeyValuePair<string, int> wordCount in currHistogram)
            {
                int currWordCount = wordCount.Value;
                if (randSample >= currWordCount)
                {
                    randSample -= currWordCount;
                }
                else
                {
                    result = wordCount.Key;
                    break;
                }
            }

            return result;
        }
    }
}
