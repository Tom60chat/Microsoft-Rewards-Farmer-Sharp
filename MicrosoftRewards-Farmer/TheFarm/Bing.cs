using Newtonsoft.Json;
using PuppeteerSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace MicrosoftRewardsFarmer.TheFarm
{
	public class Bing
	{
		#region Constructors
		public Bing(Farmer farmer)
		{
			this.farmer = farmer;
		}
		#endregion

		#region Variables
		private const string bingLoginURL = "https://login.live.com";

		private readonly Farmer farmer;
		#endregion

		#region Methods
		public async Task LoginToMicrosoftAsync()
		{
			farmer.WriteStatus($"LogIn...");

			ElementHandle element;
			bool succes = false;

			if (!await farmer.MainPage.TryGoToAsync(bingLoginURL, WaitUntilNavigation.DOMContentLoaded))
				throw new Exception("Login: navigation failed");

			// Enter Username
			await farmer.MainPage.WaitForSelectorAsync("input[name = \"loginfmt\"]");
			while (!succes)
			{
				try
				{
					if (await farmer.MainPage.QuerySelectorAsync("input[name = \"loginfmt\"]") == null) break;

					await farmer.MainPage.ReplaceAllTextAsync("input[name = \"loginfmt\"]", farmer.Credentials.Username);
					element = await farmer.MainPage.WaitForSelectorAsync("input[id = \"idSIButton9\"]");
					await element.ClickAsync();

					succes = await farmer.MainPage.WaitForSelectorToHideAsync("input[name = \"loginfmt\"]", true, 4000);
				}
				catch (PuppeteerException) { }
			}

			// Enter password
			if (farmer.Credentials.Password != "")
			{
				succes = false;

				await farmer.MainPage.WaitForSelectorAsync("input[name = \"passwd\"]");

				while (!succes &&
					farmer.MainPage.Url.StartsWith("https://login.live.com/"))
				{
					try
					{
						if (await farmer.MainPage.QuerySelectorAsync("input[name = \"passwd\"]") == null) break;

						await farmer.MainPage.ReplaceAllTextAsync("input[name = \"passwd\"]", farmer.Credentials.Password);

						element = await farmer.MainPage.WaitForSelectorAsync("input[id = \"idSIButton9\"]");
						await element.ClickAsync();
						succes = await farmer.MainPage.WaitForSelectorToHideAsync("input[name = \"passwd\"]", true, 4000);
					}
					catch (PuppeteerException) { }
				}
			}

			await farmer.MainPage.WaitTillHTMLRendered();

			// User warning
			if (farmer.MainPage.Url.StartsWith("https://account.live.com/Abuse"))
			{
				var message = await farmer.MainPage.GetInnerTextAsync("#StartHeader");

				throw new Exception("Login error - " + message);
			}

			// Remind password
			succes = false;

			while (!succes &&
				farmer.MainPage.Url.StartsWith("https://login.live.com/"))
			{
				try
				{
					if (await farmer.MainPage.QuerySelectorAsync("input[name = \"DontShowAgain\"]") == null) break;

					await farmer.MainPage.WaitForSelectorAsync("input[name = \"DontShowAgain\"]", new WaitForSelectorOptions() { Timeout = 600000 });
					await farmer.MainPage.ClickAsync("input[id = \"idSIButton9\"]");

					succes = await farmer.MainPage.WaitForSelectorToHideAsync("input[name = \"DontShowAgain\"]", true, 4000);
					break;
				}
				catch (PuppeteerException) { }
			}

			farmer.Connected = true;
		}

		public async Task RunSearchesAsync(byte numOfSearches = 20)
		{
			farmer.WriteStatus($"Generation searches...");

			var url = "https://www.bing.com/search?q=";
			var terms = GetSearchTerms(numOfSearches);
			int i = 0;

			foreach (var term in terms)
			{
				i++;

				farmer.WriteStatus($"Running searches {i}/{numOfSearches}");
				await farmer.MainPage.TryGoToAsync(url + term, WaitUntilNavigation.Networkidle0);
				await CheckBingReady(farmer.MainPage);
			}
		}

		private string[] GetSearchTerms(uint num = 20) => RandomWord.GetWords(num);

		public async Task SwitchToMobileAsync()
		{
			var iPhone = Puppeteer.Devices[PuppeteerSharp.Mobile.DeviceDescriptorName.IPhone6];
			await farmer.MainPage.EmulateAsync(iPhone);
			farmer.Mobile = true;
		}

		public async Task SwitchToDesktopAsync()
		{
			await farmer.MainPage.SetViewportAsync(farmer.DefaultViewport);
			await farmer.MainPage.SetUserAgentAsync(farmer.DefaultUserAgent);
			farmer.Mobile = false;
		}

		public async Task CheckBingReady(Page page)
		{
			ElementHandle element;

			// If not farmer.Connected to MR, connect to MR
			element = await farmer.MainPage.QuerySelectorAsync("a[onclick=\"setsrchusr()\"]"); // Test this because their is a bug with bing (Error 400)
			if (farmer.Connected && element != null)
			{
				// This page is so wrong, I have no idea why (Micro$oft?)
				// When clicking on connect button we go into a 400 error page, but we actually sucefully connect
				// So we juste need to redirect our selft into the page we originaly want

				var encodedUrl = farmer.MainPage.Url.Remove(0, 39);  //https://www.bing.com/rewards/signin?ru= {encoded redirection link}
				var url = HttpUtility.UrlDecode(encodedUrl);

				await element.ClickAsync();
				await farmer.MainPage.WaitForSelectorToHideAsync("a[onclick=\"setsrchusr()\"]");
				await farmer.MainPage.TryGoToAsync(url, WaitUntilNavigation.Networkidle0);

				await CheckBingReady(page); // To be sure the page we wanted it's clean
				return;
			}

			// If cookies, eat it !
			element = await farmer.MainPage.QuerySelectorAsync("button[id=\"bnp_btn_accept\"]");
			if (element != null)
			{
				while (!(await element.IsVisible())) { }
				await element.ClickAsync();
			}

			// If Bing Wallpater, kill it without mercy !
			element = await farmer.MainPage.QuerySelectorAsync("span[id=\"bnp_hfly_cta2\"]");
			if (element != null)
			{
				while (!(await element.IsVisible())) { }
				await element.ClickAsync();
			}

			// If not farmer.Connected, connect to Bing
			element = await farmer.MainPage.QuerySelectorAsync("input[id=\"id_a\"]");
			if (farmer.Connected && element != null)
			{
				await element.ClickAsync();
				await farmer.MainPage.WaitForSelectorToHideAsync("input[id=\"id_a\"]");
			}

			// Oh poop
			if (farmer.MainPage.Url.StartsWith("https://account.live.com/proofs/Verify"))
				throw new Exception("Bing error - This account must be verified.");
		}
        #endregion
    }
}
