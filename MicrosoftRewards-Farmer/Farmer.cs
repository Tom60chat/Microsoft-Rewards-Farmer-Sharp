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
using System.Threading;
using System.Threading.Tasks;
using System.Web;

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
		int consoleTop;
		//int consoleLeft;
		const string defaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.82 Safari/537.36 Edg/93.0.961.52";
		ViewPortOptions defaultViewport;
		bool mobile = false;
		protected Browser browser;
		protected Page page;
        readonly Credentials credentials;
		bool farming;
		bool connected;
		uint userPoints;
		#endregion

		#region Methods
		protected async Task Init(int consoleTop = 0)
		{
			this.consoleTop = consoleTop;
			Debug.WriteLine(consoleTop);
			browser = await PuppeteerUtility.GetBrowser();

			// Gived up idea
			/*try
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
			}*/
			
			try
			{
				// Init
				page = await browser.GetCurrentPage();
				defaultViewport = page.Viewport;

				await page.SetUserAgentAsync(defaultUserAgent);
			}
			catch (Exception e)
			{
				WriteStatus("[Exception] Init: " + e.Message);
			}

			/*consoleLeft = DynamicConsole.CustomAction(
				() => ColoredConsole.Write($"<$Green;{credentials.Username}> - "),
				consoleTop,
				0
				);*/
		}

		public async Task FarmPoints(int consoleTop)
		{
			if (farming) return;

			farming = true;

			await Init(consoleTop);

			try
			{	
				await LoginToMicrosoftAsync();

				// Get points
				userPoints = await GetRewardsPointsAsync();

				/*consoleLeft = DynamicConsole.CustomAction(
					() => ColoredConsole.Write($"<$Blue;{rewardPoints} points> - "),
					consoleTop,
					consoleLeft
					);*/

				//ColoredConsole.WriteLine($"<$Green;{credentials.Username}> have {rewardPoints} points");

				// Resolve cards
				//ColoredConsole.WriteLine($"<$Green;{credentials.Username}> - [Rewards]: Beginning cards resolve.");
				await GetCardsAsync();

				// Run seaches
				//ColoredConsole.WriteLine($"<$Green;{credentials.Username}> - [BING]: Beginning searches.");
				await RunSearchesAsync((90 / 3) + 4); // 90 points max / 3 points per page

				await SwitchToMobileAsync();
				await RunSearchesAsync(60 / 3); // 60 points max / 3 points per page

				// Get end points
				var endRewardPoints = await GetRewardsPointsAsync();

				WriteStatus($"Done - Gain: {endRewardPoints - userPoints} - <$Yellow; Total: {endRewardPoints}>");

				/*Console.WriteLine("DONE!");
				Console.WriteLine();
				ColoredConsole.WriteLine($"[~~Points Summary For <$Green;{credentials.Username}> ~~]");
				ColoredConsole.WriteLine($"Total points: <$Green;{endRewardPoints}>");
				ColoredConsole.WriteLine($"Total points gained: <$Green;{endRewardPoints - rewardPoints}>");

				DisplayRedemptionOptions(endRewardPoints);*/

			}
			catch (Exception e)
			{
				Debug.WriteLine(e); // Send full error to debugger console
				//Console.WriteLine($"{credentials.Username} - {e.Message}: "); // Send error message to console

				WriteStatus("[Exception] FarmPoints: " + e.Message);
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
			if (browser != null)
			{
				farming = false;
				await browser.CloseAsync();
				await browser.DisposeAsync();
			}
		}

		protected async Task LoginToMicrosoftAsync()
		{
			WriteStatus($"LogIn...");

			var url = "https://login.live.com";
			ElementHandle element;

			if (!await page.TryGoToAsync(url, WaitUntilNavigation.DOMContentLoaded))
				throw new Exception("Login: navigation failed");

			// Enter Username
			element = await page.WaitForSelectorAsync("input[name = \"loginfmt\"]");
			await element.TypeAsync(credentials.Username);
			element = await page.WaitForSelectorAsync("input[id = \"idSIButton9\"]");
			await element.ClickAsync();

			// Enter password
			if (credentials.Password != "")
			{
				await page.WaitForSelectorAsync("input[name = \"passwd\"]");
				await page.ReplaceAllTextAsync("input[name = \"passwd\"]", credentials.Password);

				while (true)
				{
					try
					{
						element = await page.WaitForSelectorAsync("input[id = \"idSIButton9\"]");
						await element.ClickAsync();
						await page.WaitForSelectorAsync("input[name = \"passwd\"]", new WaitForSelectorOptions()
						{
							Timeout = 500,
							Hidden = true
						});
						break;
					}
					catch (PuppeteerException) { }
				}
			}

			// Don't remind password
			while (true)
			{
				try
				{
					await page.WaitForSelectorAsync("input[name = \"DontShowAgain\"]", new WaitForSelectorOptions() { Timeout = 0 });
					await page.ClickAsync("input[id = \"idSIButton9\"]");

					await page.WaitForSelectorAsync("input[name = \"DontShowAgain\"]", new WaitForSelectorOptions()
					{
						Timeout = 5000,
						Hidden = true
					});
					break;
				}
				catch (PuppeteerException) { }
			}

			connected = true;
		}

		protected async Task<uint> GetRewardsPointsAsync()
		{
			WriteStatus("Obtaining the number of current reward points...");

			if (!page.Url.StartsWith("https://www.bing.com/search"))
				await RunSearchesAsync(1); // Go to Bing and connect
			if (mobile)
			{
				await SwitchToDesktopAsync();
				await page.ReloadAsync(null, new WaitUntilNavigation[] { WaitUntilNavigation.Networkidle0 });
			}

			await page.WaitForSelectorAsync("span[id=\"id_rc\"]");
			await page.WaitTillHTMLRendered();
			//await page.WaitForTimeoutAsync(500); // Let animation finish // Need
			var pointsJson = await page.EvaluateFunctionAsync<JValue>(@"() => {
					const rewardsSel = `span[id=""id_rc""]`;
					const element = document.querySelector( rewardsSel );
					return element && element.innerText; // will return undefined if the element is not found
				}"); // Doesn't allways return points

			return uint.TryParse(pointsJson.Value.ToString().Replace(" ", ""), out var points) ? points : 0;
		}

		protected async Task GetCardsAsync()
        {
			WriteStatus("Gettings cards...");

			var url = "https://account.microsoft.com/rewards";

			await page.TryGoToAsync(url, WaitUntilNavigation.Networkidle0);

			// If not logged
			if (await page.QuerySelectorAsync("a[id=\"raf-signin-link-id\"]") != null)
			{
				// Click on the "Connect to Rewards" link
				await page.ClickAsync("a[id=\"raf-signin-link-id\"]");
			}

			await page.WaitForSelectorAsync("div[class=\"points clearfix\"]");
			var cardsElement = await page.QuerySelectorAllAsync("div[class=\"points clearfix\"]");

			int i = 0;
			foreach (var cardElement in cardsElement)
            {
				i++;
				WriteStatus($"Card processing {i} / {cardsElement.Length}");

				if (await cardElement.IsVisible(page)) // IsIntersectingViewportAsync = bad
				{
					var promise = browser.PromiseNewPage();

					await cardElement.ClickAsync();

					var cardPage = promise.Result.Result; // This is stupid
					if (cardPage == null)
						throw new NullReferenceException(
							credentials.Username + " - Can't find the new page");

					await cardPage.WaitTillHTMLRendered();
					await CheckBingReady(cardPage);
					await cardPage.WaitTillHTMLRendered();
					await ProceedCard(cardPage);

					promise = browser.PromiseNewPage();
					await cardPage.CloseAsync();
					promise.GetAwaiter().GetResult().GetAwaiter().GetResult(); // This is even more stupid
				}
            }
		}

		protected async Task ProceedCard(Page cardPage)
		{
			// Wait quest pop off
			//await page.WaitForTimeoutAsync(500);
			ElementHandle element;

			// Poll quest (Don't support multi quest)
			if ((element = await cardPage.QuerySelectorAsync("div[id^=\"btoption\"]")) != null) // click 
			{
				// Click on the first option (I wonder why it's always the first option that is the most voted 🤔)

				await element.ClickAsync();
				await page.WaitForSelectorToHideAsync("div[id^=\"btoption\"]");

				// should do better
				/*if ((element = await cardPage.QuerySelectorAsync("div[id^=\"btoption\"]")) != null) // click 
				{
					await element.ClickAsync();
				}*/
			}

			// 50/50 & Quiz // TODO: make it smart
			if ((element = await cardPage.QuerySelectorAsync("input[id=\"rqStartQuiz\"]")) != null)
			{
				await element.ClickAsync();
			}
			// Quiz & 50/50
			while ((element = await cardPage.QuerySelectorAsync("div[id^=\"rqAnswerOption\"]")) != null) // click 
			{
				await CheckBingReady(cardPage);

				while (!await element.IsVisible(cardPage))
				{
					if (await cardPage.QuerySelectorAsync("div[class=\"cico rqSumryLogo \"]") != null)
						return;
				}

				await element.ClickAsync();

				while (true)
				{
					try
					{
						await cardPage.WaitForSelectorAsync("div[id^=\"rqAnswerOption\"]", new WaitForSelectorOptions()
						{
							Timeout = 500,
							Hidden = false
						});
						break;
					}
					catch (PuppeteerException)
					{
						if (await cardPage.QuerySelectorAsync("div[class=\"cico rqSumryLogo \"]") != null)
							return;
					}
				}
			}
			// Quick quiz
			while ((element = await cardPage.QuerySelectorAsync("input[class=\"rqOption\"]")) != null) // click 
			{
				await CheckBingReady(cardPage);

				while (!await element.IsVisible(cardPage))
				{
					if (await cardPage.QuerySelectorAsync("div[class=\"cico rqSumryLogo \"]") != null)
						return;
				}

				await element.ClickAsync();

				while (true)
				{
					try
					{
						await cardPage.WaitForSelectorAsync("input[class=\"rqOption\"]", new WaitForSelectorOptions()
						{
							Timeout = 500,
							Hidden = false
						});
						break;
					}
					catch (PuppeteerException)
					{
						if (await cardPage.QuerySelectorAsync("div[class=\"cico rqSumryLogo \"]") != null)
							return;
					}
				};
				/*await cardPage.WaitTillHTMLRendered(5000);
				await cardPage.WaitForTimeoutAsync(500);*/
			}
		}

		protected async Task RunSearchesAsync(byte numOfSearches = 20)
		{
			WriteStatus($"Generation searches...");

			var url = "https://www.bing.com/search?q=";
			var terms = GetSearchTerms(numOfSearches);
			int i = 0;

			/*var sb = new StringBuilder();
			sb.Append("[");
			foreach (var term in terms)
				sb.Append
					($"<$Green;{term}>,");
			sb.AppendLine("]");
			ColoredConsole.Write(sb.ToString());*/

			foreach (var term in terms)
			{
				i++;

				WriteStatus($"Running searches {i}/{numOfSearches}");
				await page.TryGoToAsync(url + term, WaitUntilNavigation.Load);
				await CheckBingReady(page);
			}
		}

		private string[] GetSearchTerms(byte num = 20)
		{
			
			var url = $"https://random-word-api.herokuapp.com/word?swear=0&number={num}";

			var jsonTerms = new WebClient().DownloadString(url);
			var array = JArray.Parse(jsonTerms);
			
			return array.Values<string>().ToArray();

			// Less hummain, but use less data to use
			// TODO
			/*for (int i = 0; i < num; i++)
			{
				var sb = new StringBuilder();
				var rand = new Random();

				for (int j = 0; j < rand.Next(3, 12); j++)
					rand.Next('A', 'z');
			}*/
			
		}

		protected async Task SwitchToMobileAsync()
		{
			var iPhone = Puppeteer.Devices[PuppeteerSharp.Mobile.DeviceDescriptorName.IPhone6];
			await page.EmulateAsync(iPhone);
			mobile = true;
		}

		protected async Task SwitchToDesktopAsync()
		{
			await page.SetViewportAsync(defaultViewport);
			await page.SetUserAgentAsync(defaultUserAgent);
			mobile = false;
		}

		protected void DisplayRedemptionOptions(uint points)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"Your point value of <$Green;{points}> is roughly equal to:");
			sb.AppendLine();

			foreach (var reward in Program.Settings.Rewards)
			{
				sb.AppendLine($"\t<$Blue;{points * 100 / reward.Cost}%> of <$White;{reward.Title}> (<$Green;{reward.Cost}> pts)");
				sb.AppendLine($"\tor <$Blue;{points * 100 / reward.Discounted}%> of <$Green;{reward.Discounted}> pts at the discounted Level 2 rate");
				sb.AppendLine();
			}

			ColoredConsole.WriteLine(sb.ToString());
		}

		private async Task CheckBingReady(Page page)
		{
			ElementHandle element;

			// If not connected to MR, connect to MR
			element = await page.QuerySelectorAsync("a[onclick=\"setsrchusr()\"]"); // Test this because their is a bug with bing (Error 400)
			if (connected && element != null)
			{
				// This page is so wrong, I have no idea why (Micro$oft?)
				// When clicking on connect button we go into a 400 error page, but we actually sucefully connect
				// So we juste need to redirect our selft into the page we originaly want

				var encodedUrl = page.Url.Remove(0, 39);  //https://www.bing.com/rewards/signin?ru= {encoded redirection link}
				var url = HttpUtility.UrlDecode(encodedUrl);

				await element.ClickAsync();
				await page.WaitForSelectorToHideAsync("a[onclick=\"setsrchusr()\"]");
				await page.TryGoToAsync(url, WaitUntilNavigation.Networkidle0);

				await CheckBingReady(page); // To be sure the page we wanted it's clean
				return;
			}

			// If cookies, eat it !
			//bnp_btn_accept
			element = await page.QuerySelectorAsync("button[id=\"bnp_btn_accept\"]");
			if (element != null)
			{
				while (!(await element.IsVisible(page))) { }
				await element.ClickAsync();
			}

			// If Bing Wallpater, kill it without mercy !
			element = await page.QuerySelectorAsync("span[id=\"bnp_hfly_cta2\"]");
			if (element != null)
			{
				while (!(await element.IsVisible(page))) { }
				await element.ClickAsync();
			}

			// If not connected, connect to Bing
			element = await page.QuerySelectorAsync("input[id=\"id_a\"]");
			if (connected && element != null)
			{
				await element.ClickAsync();
				//await page.WaitForSelectorToHideAsync("input[id=\"id_a\"]"); // may not needed ?
			}
		}

		private void WriteStatus(string status)
        {
			string name = credentials.Username.Substring(0, credentials.Username.IndexOf('@'));
			string points = $"{userPoints} point";
			string value =
				$"<$Green;{name}><$Gray; - ><$Blue;{points}><$Gray;: >{status}";

			DynamicConsole.ClearLine(consoleTop);
			DynamicConsole.CustomAction(
						() => ColoredConsole.WriteLine(value),
						0, consoleTop);
		}
		#endregion
	}
}
