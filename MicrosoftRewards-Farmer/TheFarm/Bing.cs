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

			if (!await farmer.MainPage.TryGoToAsync(bingLoginURL, WaitUntilNavigation.DOMContentLoaded))
				throw new Exception("Login: navigation failed");

			// Enter Username
			await farmer.MainPage.WaitForSelectorAsync("input[name = \"loginfmt\"]");
			await FillInLoginForm("input[name = \"loginfmt\"]", farmer.Credentials.Username, "#usernameError");

			// Enter password
			if (farmer.Credentials.Password != "")
				await FillInLoginForm("input[name = \"passwd\"]", farmer.Credentials.Password, "#passwordError");
			else
				await farmer.MainPage.WaitForSelectorToHideAsync("input[name = \"passwd\"]", true, 0);

			await farmer.MainPage.WaitTillHTMLRendered();

			// User warning
			if (farmer.MainPage.Url.StartsWith("https://account.live.com/Abuse"))
			{
				var message = await farmer.MainPage.GetInnerTextAsync("#StartHeader");

				throw new Exception("Login error - " + message);
			}

			// Oops
			element = await farmer.MainPage.QuerySelectorAsync("#idTD_Error");
			if (element != null)
			{
				var messageTitle = await farmer.MainPage.GetInnerTextAsync("#idTD_Error");
				var messageInfo = await farmer.MainPage.GetInnerTextAsync("#error_Info");
				throw new Exception("Login error - " + messageTitle + " " + messageInfo);
			}

			// Remind password
			var succes = false;

			while (!succes && farmer.MainPage.Url.StartsWith("https://login.live.com/"))
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

		private async Task FillInLoginForm(string selector, string value, string errorSelector)
		{
			ElementHandle element;
			bool succes = false;

			while (!succes && farmer.MainPage.Url.StartsWith("https://login.live.com/"))
			{
				try
				{
					if (await farmer.MainPage.QuerySelectorAsync(selector) == null) break;

					await farmer.MainPage.ReplaceAllTextAsync(selector, value);
					element = await farmer.MainPage.WaitForSelectorAsync("input[id = \"idSIButton9\"]");
					await element.ClickAsync();

					succes = await farmer.MainPage.WaitForSelectorToHideAsync(selector, true, 4000);

					// Check for error msg
					element = await farmer.MainPage.QuerySelectorAsync(errorSelector);
					if (element != null)
					{
						var error = await farmer.MainPage.GetInnerTextAsync(errorSelector);

						throw new Exception("Login error - " + error);
					}
				}
				catch (PuppeteerException) { }
			}
		}

		public async Task RunSearchesAsync(byte numOfSearches = 20)
		{
			farmer.WriteStatus($"Generation {(farmer.Mobile ? "mobile" : "desktop")} searches...");

			var url = "https://www.bing.com/search?q=";
			var terms = GetSearchTerms(numOfSearches);
			int i = 0;

			foreach (var term in terms)
			{
				i++;

				farmer.WriteStatus($"Running {(farmer.Mobile ? "mobile" : "desktop")} searches {i}/{numOfSearches}");
				await farmer.MainPage.TryGoToAsync(url + term, WaitUntilNavigation.Networkidle0);
				await CheckBingReady();
			}
		}

		public async Task<bool> GoToBingAsync()
		{
			if (await farmer.MainPage.TryGoToAsync("https://www.bing.com/", WaitUntilNavigation.Networkidle0))
			{
				await farmer.MainPage.WaitTillHTMLRendered();
				await CheckBingReady();
				return true;
			}
			return false;
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

		public async Task CheckBingReady()
		{
			ElementHandle element;

			if (farmer.Mobile)
			{
				/*// Open hamburger
				element = await farmer.MainPage.QuerySelectorAsync("#mHamburger");
				if (element != null)
				{
					await element.ClickAsync();

					// If not connected, connect to Bing
					element = await farmer.MainPage.WaitForSelectorAsync("#hb_s", new WaitForSelectorOptions { Timeout = 2000 });
					if (farmer.Connected && element != null)
					{
						await farmer.MainPage.AltClickAsync("#hb_s");
						await farmer.MainPage.WaitForSelectorToHideAsync("#hb_s", true);
						//await farmer.MainPage.TryGoToAsync("https://www.bing.com/fd/auth/signin?action=interactive&provider=windows_live_id&return_url=https%3a%2f%2fwww.bing.com");
						await farmer.MainPage.WaitTillHTMLRendered();
					}
					else
					{
						// Close hamburger
						element = await farmer.MainPage.QuerySelectorAsync("#mHamburger");
						if (element != null)
							await element.ClickAsync();
					}
				}

				// If cookies, eat it ! (If the methode is called two time, this is ending a as infinit loop)
				element = await farmer.MainPage.QuerySelectorAsync("#bnp_btn_accept");
				if (element != null)
				{
					while (!(await element.IsVisible())) { }
					await element.ClickAsync();
				}*/

			}
			else
			{
				// If not connected to MR, connect to MR
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

					await CheckBingReady(); // To be sure the page we wanted it's clean
					return;
				}

				// If cookies, eat it ! (If the methode is called two time, this is ending a as infinit loop)
				element = await farmer.MainPage.QuerySelectorAsync("#bnp_btn_accept");
				if (element != null)
				{
					//while (!(await element.IsVisible())) { }
					if (await element.IsVisible())
						await element.ClickAsync();
				}

				// If Bing Wallpater, kill it without mercy !
				element = await farmer.MainPage.QuerySelectorAsync("span[id=\"bnp_hfly_cta2\"]");
				if (element != null)
				{
					//while (!(await element.IsVisible())) { }
					if (await element.IsVisible())
						await element.ClickAsync();
				}

				// If not connected, connect to Bing
				element = await farmer.MainPage.QuerySelectorAsync("#id_a");
				if (farmer.Connected && element != null && await element.IsVisible())
				{
					var url = farmer.MainPage.Url;
					await farmer.MainPage.TryGoToAsync("https://www.bing.com/fd/auth/signin?action=interactive&provider=windows_live_id&return_url=https%3a%2f%2fwww.bing.com", WaitUntilNavigation.Networkidle0);
					await farmer.MainPage.WaitTillHTMLRendered();

					if (farmer.MainPage.Url.StartsWith("https://login.live.com/"))
					{
						await LoginToMicrosoftAsync();
					}
					/*await farmer.MainPage.ClickAsync("#id_a");
					await farmer.MainPage.WaitForSelectorToHideAsync("#id_a");*/

					await farmer.MainPage.TryGoToAsync(url, WaitUntilNavigation.Networkidle0);
					await CheckBingReady(); // To be sure the page we wanted it's clean
				}

				// Oh poop
				if (farmer.MainPage.Url.StartsWith("https://account.live.com/proofs/Verify"))
					throw new Exception("Bing error - This account must be verified.");
			}
		}
        #endregion
    }
}
