using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using PuppeteerSharp.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MicrosoftRewardsFarmer
{
    public class Farmer
	{
		#region Constructors
		private Farmer() { }

		public Farmer(Credentials credentials)
		{
			this.credentials = credentials;
		}
		#endregion

		#region Variables
		const string defaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.82 Safari/537.36 Edg/93.0.961.52";
		ViewPortOptions defaultViewport;
		bool mobile = false;
		Browser browser;
		Page page;
        readonly Credentials credentials;
		bool farming;
		#endregion

		#region Methods
		public async Task FarmPoints()
		{
			if (farming) return;

			farming = true;

			try
			{
				if (Program.KeepBrowserAlive && File.Exists("lastBrowserWS.txt"))
				{
					var wsEndPoint = File.ReadAllText("lastBrowserWS.txt");
					browser = await Puppeteer.ConnectAsync(new ConnectOptions()
					{
						BrowserWSEndpoint = wsEndPoint
					});
				}
				else
				{
					browser = await PuppeteerUtility.GetBrowser();
					if (Program.KeepBrowserAlive)
						File.WriteAllText("lastBrowserWS.txt", browser.WebSocketEndpoint);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				browser = await PuppeteerUtility.GetBrowser();
			}

			try
			{
				page = await browser.GetCurrentPage();
				defaultViewport = page.Viewport;

				await page.SetUserAgentAsync(defaultUserAgent);
				

				await LoginToMicrosoftAsync();

				//var rewards = await GetRewardsInfoAsync();
				var rewards = await GetRewardsPointsAsync();

				Console.WriteLine(rewards);
				Console.WriteLine($"{credentials.Username} - [BING]: Beginning searches.");

				await GetCardsAsync();

				await RunSearchesAsync((90 / 3) + 4); // 90 points max / 3 points per page

				await SwitchToMobileAsync();
				await RunSearchesAsync(60 / 3); // 60 points max / 3 points per page

				//var endRewards = await GetRewardsInfoAsync();
				var endRewards = await GetRewardsPointsAsync();
				Console.WriteLine(endRewards);

				Console.WriteLine("DONE!");
				Console.WriteLine();
				Console.WriteLine($"[~~Points Summary For {credentials.Username} ~~]");

				DisplayRedemptionOptions(endRewards);

			}
			catch (Exception e)
			{
				Debug.WriteLine(e); // Send full error to Visual Studio console
				Console.WriteLine($"{credentials.Username} - {e.Message}"); // Send error message to console
			}
			finally
			{
				if (!Program.KeepBrowserAlive)
					//browser.Dispose();
				//else
				{
					await browser.CloseAsync();
					await browser.DisposeAsync();
				}
				farming = false;
			}
		}

		public async Task StopAsync()
		{
			if (farming && browser != null)
			{
				farming = false;
				await browser.CloseAsync();
				await browser.DisposeAsync();
			}
		}

		private async Task LoginToMicrosoftAsync()
		{
			var url = "https://login.live.com";

			await page.GoToAsync(url);

			// Enter Username, then wait 2 seconds for next card to load
			await page.WaitForSelectorAsync("input[name = \"loginfmt\"]");
			await page.TypeAsync("input[name = \"loginfmt\"]", credentials.Username);
			await page.ClickAsync("input[id = \"idSIButton9\"]");

			// Enter password, then wait 2 seconds for next card to load
			if (credentials.Password != "")
			{
				await page.WaitForSelectorAsync("input[name = \"passwd\"]");
				//await page.WaitForTimeoutAsync(500); // Wait the animation to finish
				await page.WaitTillHTMLRendered();
				await page.TypeAsync("input[name = \"passwd\"]", credentials.Password);
				await page.ClickAsync("input[id = \"idSIButton9\"]");
			}

			// Don"t remind password, wait for next page to load.
			await page.WaitForSelectorAsync("input[name = \"DontShowAgain\"]", new WaitForSelectorOptions() { Timeout = 0 });
			await page.ClickAsync("input[id = \"idSIButton9\"]");
		}

		/*private async Task<uint> GetRewardsInfoAsync()
		{
			var url = "https://account.microsoft.com/rewards";

			await page.GoToAsync(url);

			// If not logged
			if (await page.QuerySelectorAsync("a[id=\"raf-signin-link-id\"]") != null)
			{
				// Click on the connect to Rewards link
				await page.ClickAsync("a[id=\"raf-signin-link-id\"]");

				// Wait for next page to load.
				await page.WaitForTimeoutAsync(2000);
			}

			await page.WaitForTimeoutAsync(2000); // Let animation finish

			var rewards = await page.EvaluateFunctionAsync<JValue>(@"() => {
					const rewardsSel = `#userBanner > mee-banner > div > div > div > div.info-columns > div:nth-child(1) > mee-banner-slot-2 > mee-rewards-user-status-item > mee-rewards-user-status-balance > div > div > div > div > div > p.bold.number.margin-top-1 > mee-rewards-counter-animation > span`;
					const element = document.querySelector( rewardsSel );
					return element && element.innerText; // will return undefined if the element is not found
				}");

			//Debug.WriteLine(rewards.Value.ToString().Replace(" ", ""));

			return Convert.ToUInt32(rewards.Value.ToString().Replace(" ", ""));
		}*/

		/*private async Task GoToBingAsync()
		{
			var url = "https://www.bing.com/search?q=";

			await page.GoToAsync(url);

			if (await page.QuerySelectorAsync("input[id=\"id_a\"]") != null)
			{
				await page.ClickAsync("input[id=\"id_a\"]");
				await page.WaitForTimeoutAsync(2000);
			}
		}*/

		private async Task<uint> GetRewardsPointsAsync()
		{
			if (!page.Url.StartsWith("https://www.bing.com/search"))
				await RunSearchesAsync(1); // Go to Bing and connect
			if (mobile)
			{
				await SwitchToDesktopAsync();
				await page.ReloadAsync();
			}

			await page.WaitForSelectorAsync("span[id=\"id_rc\"]");
			await page.WaitTillHTMLRendered();
			//await page.WaitForTimeoutAsync(2000); // Let animation finish // Need
			var pointsJson = await page.EvaluateFunctionAsync<JValue>(@"() => {
					const rewardsSel = `span[id=""id_rc""]`;
					const element = document.querySelector( rewardsSel );
					return element && element.innerText; // will return undefined if the element is not found
				}");

			Debug.WriteLine(pointsJson.Value.ToString().Replace(" ", ""));

			return Convert.ToUInt32(pointsJson.Value.ToString().Replace(" ", ""));
		}

		private async Task GetCardsAsync()
        {
			var url = "https://account.microsoft.com/rewards";

			await page.GoToAsync(url);

			// If not logged
			await page.WaitForTimeoutAsync(2000); // Let page load
			if (await page.QuerySelectorAsync("a[id=\"raf-signin-link-id\"]") != null)
			{
				// Click on the connect to Rewards link
				await page.ClickAsync("a[id=\"raf-signin-link-id\"]");

				// Wait for next page to load.
			}

			//div class="c-card-content"

			await page.WaitForTimeoutAsync(2000);

			var cardsElement = await page.QuerySelectorAllAsync("div[class=\"points clearfix\"]"); // points clearfix //c-card-content

			foreach (var cardElement in cardsElement)
            {
				if (await cardElement.IsVisible(page)) // IsIntersectingViewportAsync = bad
				{
					await cardElement.ClickAsync();
					var cardPage = await browser.WaitAndGetNewPage();
					await cardPage.WaitTillHTMLRendered(); // Timeout ?
					await ProceedCard(cardPage);
					if (cardPage.Url != page.Url)
                    {
						await cardPage.CloseAsync();
					}
				}
			}
		}

		private async Task ProceedCard(Page cardPage)
		{
			// Wait quest pop off
			//await page.WaitForTimeoutAsync(500);
			ElementHandle element;

			// Poll quest (Don't support multi quest)
			if ((element = await cardPage.QuerySelectorAsync("div[id=\"btoption0\"]")) != null) // click 
			{
				// Click on the first option (I wonder why it's always the first option that is the most voted 🤔)

				await element.ClickAsync();
				await page.WaitTillHTMLRendered();

				// should do better
				if ((element = await cardPage.QuerySelectorAsync("div[id=\"btoption0\"]")) != null) // click 
				{
					await element.ClickAsync();
				}
			}

			// 50/50 // TODO: make it smart
			if (await cardPage.QuerySelectorAsync("input[id=\"rqStartQuiz\"]") != null)
			{
				await cardPage.ClickAsync("input[id=\"rqStartQuiz\"]");
			}
			while ((element = await cardPage.QuerySelectorAsync("div[id=\"rqAnswerOption0\"]")) != null) // click 
			{
				if (await element.IsIntersectingViewportAsync())
				{
					await element.ClickAsync();
					await cardPage.WaitTillHTMLRendered();
				}
			}
		}


		private async Task RunSearchesAsync(byte numOfSearches = 20)
		{
			var url = "https://www.bing.com/search?q=";

			var terms = GetSearchTerms(numOfSearches);

			Console.Write("[");
			foreach (var term in terms)
				ColoredConsole.Write($"<$Green;{term}>,");
			Console.WriteLine("]");

			foreach (var term in terms)
			{
				await page.GoToAsync(url + term);
				await page.WaitTillHTMLRendered(); // Slower but saffer

				// If not, connect to Bing, wait for next page to load.
				if (await page.QuerySelectorAsync("input[id=\"id_a\"]") != null)
				{
					//await page.WaitForTimeoutAsync(500); // Wait for bing to finish loading properly
					await page.ClickAsync("input[id=\"id_a\"]");
				}
				// If cookies, eat it !
				//bnp_btn_accept
				if (await page.QuerySelectorAsync("button[id=\"bnp_btn_accept\"]") != null)
				{
					//await page.WaitForTimeoutAsync(500); // Wait for bing to finish loading properly
					await page.ClickAsync("button[id=\"bnp_btn_accept\"]");
				}
			}
		}

		private string[] GetSearchTerms(byte num = 20)
		{
			var url = $"https://random-word-api.herokuapp.com/word?swear=0&number={num}";

			var jsonTerms = new WebClient().DownloadString(url);
			var array = JArray.Parse(jsonTerms);
			
			return array.Values<string>().ToArray();
		}

		private async Task SwitchToMobileAsync()
		{
			var iPhone = Puppeteer.Devices[PuppeteerSharp.Mobile.DeviceDescriptorName.IPhone6];
			await page.EmulateAsync(iPhone);
			mobile = true;
		}
		private async Task SwitchToDesktopAsync()
		{
			await page.SetViewportAsync(defaultViewport);
			await page.SetUserAgentAsync(defaultUserAgent);
			mobile = false;
		}

		private void DisplayRedemptionOptions(uint points)
		{

			ColoredConsole.WriteLigne($"Your point value of <$Green;{points}> is roughly equal to:");
			Console.WriteLine();

			foreach (var reward in Program.Settings.Rewards)
			{
				ColoredConsole.WriteLigne($"\t<$Blue;{points * 100 / reward.Cost}%> of <$White;{reward.Title}> (<$Green;{reward.Cost}> pts)");
				ColoredConsole.WriteLigne($"\tor <$Blue;{points * 100 / reward.Discounted}%> of <$Green;{reward.Discounted}> pts at the discounted Level 2 rate");
				Console.WriteLine();
			}
		}
		#endregion
	}
}
