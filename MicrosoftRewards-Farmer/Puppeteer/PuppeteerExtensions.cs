using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using PuppeteerSharp.Input;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MicrosoftRewardsFarmer
{
    public static class PuppeteerExtensions
    {
        public static async Task<Page> GetCurrentPage(this Browser browser)
        {
            var pages = await browser.PagesAsync();
            return pages[0]; // TODO: Find current page
        }

        public static Task<Page> PromiseNewPage(this Browser browser, int timeout = 30000)
        {
            var tokenSource = new CancellationTokenSource();
            if (timeout != 0)
                tokenSource.CancelAfter(timeout);
            return PromiseNewPage(browser, tokenSource.Token);
        }

        public static Task<Page> PromiseNewPage(this Browser browser, CancellationToken token)
        {
            return Task.Factory.StartNew(() =>
            {
                var pages = browser.PagesAsync().GetAwaiter().GetResult() ;
                Page[] newPages = pages;
                int pagesCount = pages.Length;

                while (
                    !token.IsCancellationRequested &&
                    (newPages = browser.PagesAsync().GetAwaiter().GetResult()).Length == pagesCount)
                { }

                if (token.IsCancellationRequested)
                    return null;

                var diff = newPages.Except(pages).ToList();
                return diff.FirstOrDefault();
            });
        }

        // https://github.com/puppeteer/puppeteer/issues/4356#issuecomment-487330171
        public static async Task<bool> IsVisible(this ElementHandle elementHandle) => await elementHandle.EvaluateFunctionAsync<bool>(@"(el) => {
                if (!el || el.offsetParent === null)
                    return false;

                const style = window.getComputedStyle(el);
                return style && style.display !== 'none' && style.visibility !== 'hidden' && style.opacity !== '0';
            }", elementHandle);

        // https://stackoverflow.com/a/61304202
        public static async Task WaitTillHTMLRendered(this Page page, int timeout = 30000)
        {
            var checkDurationMsecs = 500;
            var maxChecks = timeout / checkDurationMsecs;
            var lastHTMLSize = 0;
            var checkCounts = 1;
            var countStableSizeIterations = 0;
            var minStableSizeIterations = 3;

            while (checkCounts++ <= maxChecks)
            {
                try
                {
                    var html = await page.GetContentAsync();
                    var currentHTMLSize = html.Length;

                    if (lastHTMLSize != 0 && currentHTMLSize == lastHTMLSize)
                        countStableSizeIterations++;
                    else
                        countStableSizeIterations = 0; //reset the counter

                    if (countStableSizeIterations >= minStableSizeIterations)
                    {
                        break;
                    }

                    lastHTMLSize = currentHTMLSize;
                    await page.WaitForTimeoutAsync(checkDurationMsecs);
                } catch (EvaluationFailedException)
                {
                    continue; // Brute force
                }
            }
        }

        /// <summary>
        /// Wait for a page to exit
        /// </summary>
        /// <param name="page"></param>
        /// <param name="targetUrl">The URL or start URL to wait for his exit</param>
        /// <param name="timeout">Time to wait before stop waiting</param>
        public static Task WaitForPageToExit(this Page page, string targetUrl = null, int timeout = 30000)
        {
            if (targetUrl == null)
                targetUrl = page.Url;

            var tokenSource = new CancellationTokenSource();
            if (timeout != 0)
                tokenSource.CancelAfter(timeout);
            return WaitForPageToExit(page, targetUrl, tokenSource.Token);
        }

        /// <summary>
        /// Wait for a page to exit
        /// </summary>
        /// <param name="page"></param>
        /// <param name="targetUrl">The URL or start URL to wait for his exit</param>
        /// <param name="token">Cancellation token</param>
        public static async Task WaitForPageToExit(this Page page, string targetUrl, CancellationToken token)
        {
            while (page.Url.StartsWith(targetUrl))
            {
                await Task.Delay(500);
                if (token.IsCancellationRequested)
                    break;
            }
        }

        /// <summary>
        /// Try navigates to an url and promise that the navigation work.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <param name="options">Navigation parameters.</param>
        /// <param name="maxTry">Number of time to retry when navigation fail</param>
        /// <returns>If navigation success</returns>
        public static async Task<bool> TryGoToAsync(this Page page, string url, NavigationOptions options, int maxTry = 10)
        {
            Response response;
            int trys = 0;

            if (page == null || string.IsNullOrEmpty(url))
                return false;

            while (maxTry >= trys)
            {
                try
                {
                    response = await page.GoToAsync(url, options);
                    if (response != null && response.Ok)
                        return true;

                    // May not be needed
                    /*else if (response == null)  // NULL can happen when there is a redirection before, TODO: Check if the target page has been loaded
                        if (page.Url.StartsWith(url))
                            return true;
                        else
                        {
                            Debug.WriteLine("wait for: " + page.Url);
                            await page.WaitForPageToExit();
                            if (page.Url.StartsWith(url))
                                return true;
                        }*/
                }
                catch (Exception) { }

                trys++;
            }

            return false;
        }

        /// <summary>
        /// Try navigates to an url and promise that the navigation work.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <param name="timeout">Maximum navigation time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.</param>
        /// <param name="waitUntil">
        ///     When to consider navigation succeeded, defaults to PuppeteerSharp.WaitUntilNavigation.Load.
        ///     Given an array of PuppeteerSharp.WaitUntilNavigation, navigation is considered
        ///     to be successful after all events have been fired</param>
        /// <returns>If navigation success</returns>
        public static async Task<bool> TryGoToAsync(this Page page, string url, int? timeout = null, WaitUntilNavigation[] waitUntil = null) => await page.TryGoToAsync(
            url,
            new NavigationOptions()
            {
                Timeout = timeout,
                WaitUntil = waitUntil
            },
            1);


        /// <summary>
        /// Try navigates to an url and promise that the navigation work.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <param name="waitUntil">When to consider navigation succeeded.</param>
        /// <param name="maxTry">Number of time to retry when navigation fail</param>
        /// <returns>If navigation success</returns>
        public static async Task<bool> TryGoToAsync(this Page page, string url, WaitUntilNavigation waitUntil, int maxTry = 10) => await page.TryGoToAsync(
            url,
            new NavigationOptions()
            {
                WaitUntil = new WaitUntilNavigation[] { waitUntil }
            },
            maxTry);

        /// <summary>
        /// Clear the text annd sends a keydown, keypress/input, and keyup event for each character in the text.
        /// </summary>
        /// <param name="selector">
        /// A selector to search for element to focus. If there are multiple elements satisfying the selector,
        /// the first will be focused.
        /// </param>
        /// <param name="text">A text to type into a focused element</param>
        /// <param name="options">type options</param>
        /// <returns>Task</returns>
        public static async Task ReplaceAllTextAsync(this Page page, string selector, string text, TypeOptions options = null)
        {
            ElementHandle element;

            while (true)
            {
                await page.FocusAsync(selector);
                await page.Keyboard.DownAsync("Control");
                await page.Keyboard.PressAsync("A");
                await page.Keyboard.UpAsync("Control");
                await page.Keyboard.PressAsync("Backspace");
                await page.Keyboard.TypeAsync(text, options);


                element = await page.QuerySelectorAsync(selector);
                if (element != null)
                {
                    var value = await page.GetValueAsync(selector);

                    if (value != text)
                        continue;
                }

                return;
            }
        }

        /// <summary>
        /// Waits for a selector to hide
        /// </summary>
        /// <param name="selector">A selector of an element to wait for</param>
        /// <param name="timeout">The time span to wait before canceling the wait</param> 
        /// <param name="visibilityCheck">wait until the element is not visible instead of disappearing</param> 
        /// <returns>Task</returns>
        public static Task<bool> WaitForSelectorToHideAsync(this Page page, string selector, bool visibilityCheck = false, int timeout = 30000)
        {
            var tokenSource = new CancellationTokenSource();
            if (timeout != 0)
                tokenSource.CancelAfter(timeout);
            return WaitForSelectorToHideAsync(page, selector, tokenSource.Token, visibilityCheck);
        }

        /// <summary>
        /// Waits for a selector to hide
        /// </summary>
        /// <param name="selector">A selector of an element to wait for</param>
        /// <returns>Task</returns>
        private static async Task<bool> WaitForSelectorToHideAsync(this Page page, string selector, CancellationToken cancellationToken, bool visibilityCheck = false)
        {
            ElementHandle element;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    element = await page.QuerySelectorAsync(selector);
                    if (element != null)
                    {
                        if (visibilityCheck && !await element.IsVisible())
                            return true;

                        await Task.Delay(500);
                    }
                    else
                        return true;
                }
                catch (PuppeteerException) { break; }
                catch (NullReferenceException) { break; }
            };

            return false;
        }

        public static Task<JToken> RemoveAsync(this Page page, string selector) => page.EvaluateFunctionAsync(@"(sel) =>
        {
			var elements = document.querySelectorAll(sel);
			for (var i=0; i< elements.length; i++) {
				elements[i].parentNode.removeChild(elements[i]);
			}
		}", selector);

        public static Task<JToken> AltClickAsync(this Page page, string selector) => page.EvaluateFunctionAsync(@"(sel) =>
        {
            document.querySelector(sel).click();
		}", selector);

        public static async Task<string> GetInnerTextAsync(this Page page, string selector)
        {
            var element = await page.EvaluateFunctionAsync<JValue>(@"(sel) => {
					        const element = document.querySelector(sel);
					        return element && element.innerText; // will return undefined if the element is not found
		        }", selector);

            return element != null ? element.Value.ToString() : null;
        }

        public static async Task<string> GetValueAsync(this Page page, string selector)
        {
            var element = await page.EvaluateFunctionAsync<JValue>(@"(sel) => {
					        const element = document.querySelector(sel);
					        return element && element.value; // will return undefined if the element is not found
		        }", selector);

            return element != null ? element.Value.ToString() : null;
        }

        public static void SetConsoleOutput(this Page page) =>
            page.Console += async (_, msg) => {
                const string TAG = "[JS Console] ";

                if (msg.Message.Args != null)
                    foreach (var msgArg in msg.Message.Args)
                        try { Debug.WriteLine(TAG + await msgArg.JsonValueAsync()); } catch { }

                /*if (msg.Message.Text != null)
                    Debug.WriteLine(TAG + msg.Message.Text);*/
            };
    }
}
