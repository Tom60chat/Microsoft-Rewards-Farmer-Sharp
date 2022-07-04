using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MicrosoftRewardsFarmer
{
    public static class PuppeteerUtility
    {
        public static async Task<string> GetBrowser()
        {
            if (TryGetUserBrowser(out var browserUserPath))
                return browserUserPath;
            else if (TryGetAppBrowser(out var browserAppPath))
                return browserAppPath;
            else
            {
                Console.WriteLine("Downloading browser...");
                var browserDownloaded = await new BrowserFetcher().DownloadAsync();
                return browserDownloaded.ExecutablePath;
            }
        }
        public static async Task<Browser> StartNewBrowser(string executablePath, bool headless = false)
        {
            var br = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                ExecutablePath = executablePath,
                Args = new string[] {
                    "--incognito",
                  },
                Headless = headless,
            });
            return br;
        }

        private static bool TryGetUserBrowser(out string browserPath)
        {
            browserPath = string.Empty;
            string[] userBrowserPaths = Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => new string[] {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Google\\Chrome\\Application\\chrome.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Chromium\\Application\\chrome.exe")
                },
                PlatformID.Unix => new string[] {
                    // TODO: which chrome

                    // Linux
                    "/usr/bin/chromium",
                    "/usr/bin/chrome",

                    "/usr/local/bin/chromium",
                    "/usr/local/bin/chrome",
                    // MacOS;
                    "/Applications/Google Chrome Canary.app/Contents/MacOS/Google Chrome Canary",
                    "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
                    "/Applications/Chromium.app/Contents/MacOS/Chromium",
                    // Brew
                    "/opt/homebrew/bin/chromium",
                    "/opt/homebrew/bin/chrome",
                },
                _ => Array.Empty<string>()
            };

            foreach (var userBrowserPath in userBrowserPaths)
                if (File.Exists(userBrowserPath))
                {
                    browserPath = userBrowserPath;
                    return true;
                }

            return false;
        }

        private static bool TryGetAppBrowser(out string browserPath)
        {
            browserPath = string.Empty;
            var browserFetcher = new BrowserFetcher();
            var revisions = browserFetcher.LocalRevisions();
            var revisionsEnum = revisions.GetEnumerator();

            while (revisionsEnum.MoveNext())
            {
                browserPath = browserFetcher.GetExecutablePath(revisionsEnum.Current);

                return true;
            }

            return false;
        }
    }
}
