﻿using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MicrosoftRewardsFarmer
{
    public static class PuppeteerUtility
    {
        public static async Task<Browser> GetBrowser(bool headless = false)
        {
            string executablePath = null;

            if (TryGetUserBrowser(out var browserUserPath))
                executablePath = browserUserPath;
            else if (TryGetAppBrowser(out var browserAppPath))
                executablePath = browserAppPath;
            else
                await new BrowserFetcher().DownloadAsync();

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
            var userBrowserPaths = new string[] {
                "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
                "C:\\Users\\Tom60\\AppData\\Local\\Chromium\\Application\\chrome.exe"
                };  // TODO: find installed user browsers

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
