using Newtonsoft.Json;
using PuppeteerSharp;
using System;
using System.IO;

namespace MicrosoftRewardsFarmer
{
    [Serializable]
    public class Settings
    {
        #region Variables
        public static readonly string Path = AppPath.GetFullPath(@"\Settings.json");
        #endregion

        #region Properties
        public Credentials[] Accounts { get; set; }
        public Reward[] Rewards { get; set; }
        #endregion

        public static Settings GetSettings()
        {
            if (!File.Exists(Path))
            {
                Console.WriteLine("Missing Settings.json file.");
                Console.WriteLine("This means that you need to add/recreate the Settings.json file in the root app folder.");

                if (!(Environment.OSVersion.Platform == PlatformID.Win32NT && Console.Title.EndsWith("testhost.exe")))
                {
                    Console.WriteLine();
                    Console.WriteLine("Press any button to exit.");
                    Console.ReadKey();

                    Environment.Exit(1);
                }

                throw new FileNotFoundException("Could not find Settings.json file.");
            }

            var settingsJson = File.ReadAllText(Path);
            try
            {
                return JsonConvert.DeserializeObject<Settings>(settingsJson);
            }
            catch (JsonReaderException e)
            {
                Console.WriteLine("Error loading Settings.json file.");
                Console.WriteLine("This means you need to modify the Settings.json file until it is valid.");
                Console.WriteLine();
                Console.WriteLine(e.Message);

                if (!(Environment.OSVersion.Platform == PlatformID.Win32NT && Console.Title.EndsWith("testhost.exe")))
                {
                    Console.WriteLine();
                    Console.WriteLine("Press any button to exit.");
                    Console.ReadKey();

                    Environment.Exit(1);
                }

                throw;
            }
        }
    }
}
