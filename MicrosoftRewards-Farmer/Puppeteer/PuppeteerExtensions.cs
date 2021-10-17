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

        public static async Task<Page> WaitAndGetNewPage(this Browser browser)
        {
            var pages = await browser.PagesAsync();
            Page[] newPages;
            int count = pages.Length;
            while ((newPages = await browser.PagesAsync()).Length == count) { }

            // https://stackoverflow.com/a/12795900
            var diff = newPages.Except(pages).ToList();
            return diff.FirstOrDefault();
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
            var checkDurationMsecs = 200;
            var maxChecks = timeout / checkDurationMsecs;
            var lastHTMLSize = 0;
            var checkCounts = 1;
            var countStableSizeIterations = 0;
            var minStableSizeIterations = 3;

            while (checkCounts++ <= maxChecks)
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
            }
        }
    }
}
