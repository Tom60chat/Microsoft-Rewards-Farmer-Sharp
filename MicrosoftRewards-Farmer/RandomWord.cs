﻿using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MicrosoftRewardsFarmer
{
    public static class RandomWord
    {
        public static string[] GetWords(uint numberOfWords)
        {
            try
            {
                return GetWordsLocal(numberOfWords);
            } catch { }

            try
            {
                return GetWordsOnline(numberOfWords);
            }
            catch { }

            return GetWordsGenerated(numberOfWords);
        }


        private static string[] GetWordsLocal(uint numberOfWords)
        {
            // Kinda humain and offline dunno about RAM usage (Think about 100 of instance checking for a word)

            var rand = new Random();
            string[] words = new string[numberOfWords]; // Can't find what the best between StringBuilder and string array
            long position;

            using (var file = File.OpenRead("Dictionary.txt"))
            {

                using (var streamFile = new StreamReader(file))
                {
                    for (uint i = 0; i < numberOfWords; i++)
                    {
                        position = rand.NextLong(0, file.Length);
                        file.Seek(position, SeekOrigin.Begin);

                        Debug.WriteLine(streamFile.ReadLine()); // Read partial line
                        if (streamFile.EndOfStream)
                            file.Seek(0, SeekOrigin.Begin);

                        words[i] = streamFile.ReadLine();
                    }
                }
            }

            return words;
        }

        private static string[] GetWordsOnline(uint numberOfWords)
        {
            // More humain and random but the website can't die (I mean, he's actualy die by the time I wrote this)

            var url = $"https://random-word-api.herokuapp.com/word?swear=0&number={numberOfWords}";

            var jsonTerms = new WebClient().DownloadString(url);
            var array = JArray.Parse(jsonTerms);

            return array.Values<string>().ToArray();
        }

        private static string[] GetWordsGenerated(uint numberOfWords)
        {
            // Less hummain but he will work in anyway

            var sb = new StringBuilder();
            var rand = new Random();

            for (int i = 0; i < numberOfWords; i++)
            {

                for (int j = 0; j < rand.Next(3, 12); j++)
                    sb.Append(rand.Next('A', 'z'));

                sb.AppendLine();
            }

            return sb.ToString().Split(Environment.NewLine);
        }
    }
}
