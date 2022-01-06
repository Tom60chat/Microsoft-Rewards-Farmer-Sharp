using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using PuppeteerSharp.Input;
using System;
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

        public static Task<Task<Page>> PromiseNewPage(this Browser browser, int timeout = 30000)
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(timeout);
            return PromiseNewPage(browser, tokenSource.Token);
        }

        public static Task<Task<Page>> PromiseNewPage(this Browser browser, CancellationToken token)
        {
            return Task.Factory.StartNew(async () =>
            {
                var pages = await browser.PagesAsync();
                Page[] newPages = pages;
                int pagesCount = pages.Length;

                while (
                    !token.IsCancellationRequested &&
                    (newPages = await browser.PagesAsync()).Length == pagesCount)
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
        /// Try navigates to an url and promise that the navigation work.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="url">URL to navigate page to. The url should include scheme, e.g. https://.</param>
        /// <param name="options">Navigation parameters.</param>
        /// <param name="maxTry">Number of time to retry when navigation fail</param>
        /// <returns>If navigation success</returns>
        public static async Task<bool> TryGoToAsync(this Page page, string url, NavigationOptions options, int maxTry = 10)
        {
            int trys = 0;
            bool ok = false;

            if (page == null || string.IsNullOrEmpty(url))
                return false;

            while (!ok)
            {
                if (maxTry < trys)
                    return false;

                try
                {
                    ok = (await page.GoToAsync(url, options)).Ok;
                }
                catch { }
                finally
                {
                    trys++;
                }
            }

            return true;
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
            await page.FocusAsync(selector);
            await page.Keyboard.DownAsync("Control");
            await page.Keyboard.PressAsync("A");
            await page.Keyboard.UpAsync("Control");
            await page.Keyboard.PressAsync("Backspace");
            await page.Keyboard.TypeAsync(text, options);
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

        public static async Task<string> GetInnerTextAsync(this Page page, string selector)
        {
            var element = await page.EvaluateFunctionAsync<JValue>(@"(sel) => {
					        const element = document.querySelector(sel);
					        return element && element.innerText; // will return undefined if the element is not found
		        }", selector);

            return element.Value.ToString();
        }
    }
}
