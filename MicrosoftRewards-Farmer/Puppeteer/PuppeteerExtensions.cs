using PuppeteerSharp;
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

        public static async Task<Page> WaitAndGetNewPage(this Browser browser, Page[] pages = null)
        {
            if (pages == null)
                pages = await browser.PagesAsync();
            Page[] newPages;
            int pagesCount = pages.Length;

            while ((newPages = await browser.PagesAsync()).Length == pagesCount) { }

            // https://stackoverflow.com/a/12795900
            var diff = newPages.Except(pages).ToList();
            return diff.FirstOrDefault();
        }

        public static async Task WaitNewPage(this Browser browser, Semaphore semaphore)
        {
            //semaphore = new Semaphore(0, 1);
            var pages = await browser.PagesAsync();
            int count = pages.Length;

            while ((await browser.PagesAsync()).Length == count) { }

            semaphore.Release();
        }

        public static async Task<Page> WaitAndGetNewPage(this Browser browser, Page currentPage)
        {
            Page newPage;

            while ((newPage = await browser.GetCurrentPage()).Equals(currentPage)) { }

            return newPage;
        }

        // https://github.com/puppeteer/puppeteer/issues/4356#issuecomment-487330171
        public static async Task<bool> IsVisible(this ElementHandle elementHandle, Page page) => await page.EvaluateFunctionAsync<bool>(@"(el) => {
                if (!el || el.offsetParent === null)
                {
                    return false;
                }

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

        public static async Task<bool> TryGoToAsync(this Page page, string url, NavigationOptions options)
        {
            while (!(await page.GoToAsync(url, options)).Ok) { }

            return true;
        }

        public static async Task<bool> TryGoToAsync(this Page page, string url, int? timeout = null, WaitUntilNavigation[] waitUntil = null) => await page.TryGoToAsync(url, new NavigationOptions()
        {
            Timeout = timeout,
            WaitUntil = waitUntil
        });


        public static async Task<bool> TryGoToAsync(this Page page, string url, WaitUntilNavigation waitUntil) => await page.TryGoToAsync(url, new NavigationOptions()
        {
            WaitUntil = new WaitUntilNavigation[] { waitUntil }
        });
    }
}
