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
		const string defaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.82 Safari/537.36 Edg/93.0.961.52";
		ViewPortOptions defaultViewport;
		bool mobile = false;
		protected Browser browser;
		protected Page page;
        readonly Credentials credentials;
		bool farming;
		bool connected;
		uint userPoints;
		byte progress;
		const byte totalProgress = 6;
		#endregion

		#region Methods
		protected async Task Init(int consoleTop = 0)
		{
			this.consoleTop = consoleTop;
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
		}

		public async Task FarmPoints(int consoleTop)
		{
			if (farming) return;

			farming = true;

			await Init(consoleTop);

#if !DEBUG
			try
			{
#endif
				// LogIn
				await LoginToMicrosoftAsync();
				progress++;

				// Get account points
				userPoints = await GetRewardsPointsAsync();
				progress++;

				// Resolve cards
				await GetCardsAsync();
				progress++;

				// Run seaches
				await RunSearchesAsync((90 / 3) + 4); // 90 points max / 3 points per page
				progress++;

				await SwitchToMobileAsync();
				await RunSearchesAsync(60 / 3); // 60 points max / 3 points per page
				progress++;

				// Get end points
				var endRewardPoints = await GetRewardsPointsAsync();
				progress++;

				if (userPoints == 0)
					WriteStatus($"Done - <$Yellow;Total: {endRewardPoints}>");
				else
					WriteStatus($"Done - Gain: {endRewardPoints - userPoints} - <$Yellow;Total: {endRewardPoints}>");
#if !DEBUG
			}
			catch (Exception e)
			{
				Debug.WriteLine(e); // Send full error to debugger console
				WriteStatus("[Exception] FarmPoints: " + e.Message);
			}
			finally
			{
#endif
				if (!Program.KeepBrowserAlive)
					//browser.Dispose();
				//else
				{
					await browser.CloseAsync();
					await browser.DisposeAsync();
				}
				farming = false;
#if !DEBUG
			}
#endif
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
			bool succes = false;

			if (!await page.TryGoToAsync(url, WaitUntilNavigation.DOMContentLoaded))
				throw new Exception("Login: navigation failed");

			// Enter Username
			await page.WaitForSelectorAsync("input[name = \"loginfmt\"]");
			while (!succes)
			{
				if (await page.QuerySelectorAsync("input[name = \"loginfmt\"]") == null)
					break;

				await page.ReplaceAllTextAsync("input[name = \"loginfmt\"]", credentials.Username);
				element = await page.WaitForSelectorAsync("input[id = \"idSIButton9\"]");
				await element.ClickAsync();

				succes = await page.WaitForSelectorToHideAsync("input[name = \"loginfmt\"]", true, 4000);
			}

			// Enter password
			if (credentials.Password != "")
			{
				succes = false;

				await page.WaitForSelectorAsync("input[name = \"passwd\"]");

				while (!succes && page.Url.StartsWith("https://login.live.com/"))
				{
					try
					{
						if (await page.QuerySelectorAsync("input[name = \"passwd\"]") == null)
							break;

						await page.ReplaceAllTextAsync("input[name = \"passwd\"]", credentials.Password);

						element = await page.WaitForSelectorAsync("input[id = \"idSIButton9\"]");
						await element.ClickAsync();
						succes = await page.WaitForSelectorToHideAsync("input[name = \"passwd\"]", true, 4000);
					}
					catch (PuppeteerException)
					{
						continue;
						/*if (await page.QuerySelectorAsync("input[name = \"DontShowAgain\"]") != null)
							break;*/
					}
				}
			}

			await page.WaitTillHTMLRendered();

			// User warning
			if (page.Url.StartsWith("https://account.live.com/Abuse"))
            {
				var message = await page.GetInnerTextAsync("#StartHeader");

				throw new Exception("Login error - " + message);
			}

			// Don't remind password
			succes = false;

			while (!succes && page.Url.StartsWith("https://login.live.com/"))
			{
				try
				{
					if (await page.QuerySelectorAsync("input[name = \"DontShowAgain\"]") == null)
						break;

					await page.WaitForSelectorAsync("input[name = \"DontShowAgain\"]", new WaitForSelectorOptions() { Timeout = 600000 });
					await page.ClickAsync("input[id = \"idSIButton9\"]");
					
					succes = await page.WaitForSelectorToHideAsync("input[name = \"DontShowAgain\"]", true, 4000);
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
			{
				await RunSearchesAsync(1); // Go to Bing and connect
				WriteStatus("Obtaining the number of current reward points...");
			}
			if (mobile)
			{
				await SwitchToDesktopAsync();
				await page.ReloadAsync(null, new WaitUntilNavigation[] { WaitUntilNavigation.Networkidle0 });
			}

			await page.WaitForSelectorAsync("span[id=\"id_rc\"]");
			await page.WaitTillHTMLRendered();
			//await page.WaitForTimeoutAsync(500); // Let animation finish // Need
			uint points = 0;

			while (true)
			{
				var pointsValue = await page.GetInnerTextAsync("span[id=\"id_rc\"]");

				if (uint.TryParse(pointsValue.Replace(" ", ""), out var newPoints))
				{
					if (points == newPoints)
						return points;
					else
						points = newPoints;
				}
				else
					return 0;
			}
		}

		protected async Task GetCardsAsync()
        {
			WriteStatus("Gettings cards...");

			ElementHandle element;

			await GoToMicrosoftRewardsPage();

			await page.WaitForSelectorAsync("div[class=\"points clearfix\"]");

			// Get cards
			var cardsElement = await page.QuerySelectorAllAsync("span[class=\"mee-icon mee-icon-AddMedium\"]"); // div[class=\"points clearfix\"]

			for (int i = 0; i < cardsElement.Length; i++)
			{
				element = cardsElement[i];
				WriteStatus($"Card processing {i} / {cardsElement.Length}");

				await CleanMicrosoftRewardsPage();
				if (await element.IsVisible()) // IsIntersectingViewportAsync = bad
				{
					var promise = browser.PromiseNewPage(5000);

					try
					{
						await element.ClickAsync();
					}
					catch (PuppeteerException) { continue; } // Not a HTMLElement

					var cardPage = promise.Result.Result; // This is stupid
					if (cardPage == null) // This method work very well so if the new page is null it's because theire no new page.
						continue;
						//throw new NullReferenceException("Can't find the new page");

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

		private async Task GoToMicrosoftRewardsPage()
		{
			var url = "https://account.microsoft.com/rewards";

			while (!page.Url.StartsWith("https://rewards.microsoft.com/")) // Network issue, you know
			{
				await page.TryGoToAsync(url, WaitUntilNavigation.Networkidle0);

				// Bruteforce this page
				while (page.Url.StartsWith("https://rewards.microsoft.com/welcome"))
				{
					if (await page.QuerySelectorAsync("#start-earning-rewards-link") != null) // Sign up (or sign in)
					{
						// Wait the button to be enabled
						await page.WaitForSelectorToHideAsync("a[id=\"start-earning-rewards-link\"][disabled=\"disabled\"]");

						// Click on the "Sign up to Rewards" link (Name can't be wrong I have the french version so...)
						await page.ClickAsync("#start-earning-rewards-link");
						await page.WaitForSelectorToHideAsync("#start-earning-rewards-link", false, 2000);
					}
					else // Weird... (Maybe network, it's always network)
					{
						new Exception("Can't sign in or sign up to Microsoft Rewards");
					}
				}
			}

			// Check if the account has not been ban
			if (await page.QuerySelectorAsync("#error") != null)
				await GetRawardsError();
		}

		private async Task GetRawardsError()
        {
			var errorJson = await page.GetInnerTextAsync("h1[class=\"text-headline spacer-32-bottom\"]");

			throw new Exception("Rewards error - " + errorJson);
		}

		private async Task CleanMicrosoftRewardsPage()
		{
			ElementHandle element;

			// Wellcome (75 points it's not worth) // TODO: Remove function
			await page.RemoveAsync("div[ui-view=\"modalContent\"]");
			await page.RemoveAsync("div[role=\"presentation\"]");

			// Remove pop up
			while ((element = await page.QuerySelectorAsync(".mee-icon-Cancel")) != null && await element.IsVisible())
			{
				await element.ClickAsync();
			}

			/*if ((element = await page.QuerySelectorAsync("button[class=\"c-glyph glyph-cancel\"]")) != null)
			{
				var tokenSource = new CancellationTokenSource();
				tokenSource.CancelAfter(30000);
				while (!await element.IsVisible()) { if (tokenSource.IsCancellationRequested) break; }
				if (!tokenSource.IsCancellationRequested)
					await element.ClickAsync();
			}*/
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
				await cardPage.WaitForSelectorToHideAsync("div[id^=\"btoption\"]");

				// should do better
				/*if ((element = await cardPage.QuerySelectorAsync("div[id^=\"btoption\"]")) != null) // click 
				{
					await element.ClickAsync();
				}*/

				return;
			}

			// 50/50 & Quiz
			if ((element = await cardPage.QuerySelectorAsync("input[id=\"rqStartQuiz\"]")) != null)
			{
				await element.ClickAsync();
			}
			// Quiz & 50/50 // TODO: make it smart
			if ((element = await cardPage.QuerySelectorAsync("div[id^=\"rqAnswerOption\"]")) != null)
			{
				while (element != null) // click 
				{
					await CheckBingReady(cardPage);

					while (!await element.IsVisible())
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

					element = await cardPage.QuerySelectorAsync("div[id^=\"rqAnswerOption\"]");
				}

				return;
			}
			// Quick quiz
			if ((element = await cardPage.QuerySelectorAsync("div[id^=\"rqAnswerOption\"]")) != null)
			{
				while (element != null) // click 
				{
					await CheckBingReady(cardPage);

					while (!await element.IsVisible())
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

					element = await cardPage.QuerySelectorAsync("input[class=\"rqOption\"]");
				}

				return;
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
				await page.TryGoToAsync(url + term, WaitUntilNavigation.Networkidle0);
				await CheckBingReady(page);
			}
		}

		private string[] GetSearchTerms(uint num = 20) => RandomWord.GetWords(num);

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
				while (!(await element.IsVisible())) { }
				await element.ClickAsync();
			}

			// If Bing Wallpater, kill it without mercy !
			element = await page.QuerySelectorAsync("span[id=\"bnp_hfly_cta2\"]");
			if (element != null)
			{
				while (!(await element.IsVisible())) { }
				await element.ClickAsync();
			}

			// If not connected, connect to Bing
			element = await page.QuerySelectorAsync("input[id=\"id_a\"]");
			if (connected && element != null)
			{
				await element.ClickAsync();
				await page.WaitForSelectorToHideAsync("input[id=\"id_a\"]"); // may not needed ? may needed
			}

			if (page.Url.StartsWith("https://account.live.com/proofs/Verify")) // Oh poop
				throw new Exception("Bing error - This account must be verified.");
		}

		private void WriteStatus(string status)
        {
			string name = credentials.Username.Substring(0, credentials.Username.IndexOf('@'));
			string points = $"{userPoints} point";
			string value =
				$"<$Gray;[><$Green;{name}><$Gray;](><$Cyan;{progress}/{totalProgress}><$Gray;) - ><$Blue;{points}><$Gray;: >{status}";

			DynamicConsole.ClearLine(consoleTop);
			DynamicConsole.CustomAction(
						() => ColoredConsole.WriteLine(value),
						0, consoleTop);
		}
		#endregion
	}
}
