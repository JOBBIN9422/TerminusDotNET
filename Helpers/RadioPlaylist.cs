using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace TerminusDotNetCore.Helpers
{
    public class RadioPlaylist
    {
        public string Name { get; set; }
        public string OwnerName { get; set; }
        public List<string> WhitelistUsers { get; set; }

        public LinkedList<YouTubeAudioItem> Songs { get; set; }

        public void ShuffleSongs()
        {
            //copy song list to array for index-based shuffling
            YouTubeAudioItem[] shuffleArray = new YouTubeAudioItem[Songs.Count];
            Songs.CopyTo(shuffleArray, 0);

            //shuffle song array w/ Fisher-Yates algo
            Random rand = new Random();
            for (int i = Songs.Count - 1; i >= 1; i--)
            {
                int swapIndex = rand.Next(0, i);
                YouTubeAudioItem temp = shuffleArray[i];
                shuffleArray[i] = shuffleArray[swapIndex];
                shuffleArray[swapIndex] = temp;
            }

            //copy the array back to the song list
            Songs = new LinkedList<YouTubeAudioItem>(shuffleArray);
        }
    }
}
