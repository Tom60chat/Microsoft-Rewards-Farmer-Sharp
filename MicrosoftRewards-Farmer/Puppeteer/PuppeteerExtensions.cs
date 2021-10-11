using PuppeteerSharp;
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
    }
}
