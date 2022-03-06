using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MicrosoftRewardsFarmer.TheFarm
{
	public class MsRewards
	{
		#region Constructors
		public MsRewards(Farmer farmer)
		{
			this.farmer = farmer;
		}
		#endregion

		#region Variables
		private readonly Farmer farmer;
		#endregion

		#region Methods
		public async Task<int> GetRewardsPointsAsync()
		{
			farmer.WriteStatus("Obtaining the number of current reward points...");

			if (farmer.Mobile)
			{
				await farmer.Bing.SwitchToDesktopAsync();
				if (!await farmer.Bing.GoToBingAsync())
					return 0;
			}

			if (!farmer.MainPage.Url.StartsWith("https://www.bing.com/"))
				if (!await farmer.Bing.GoToBingAsync())
					return 0;

			await farmer.MainPage.WaitTillHTMLRendered();

			return await GetRewardsHeaderBalance();
		}

		public async Task<int> GetRewardsHeaderBalance()
        {
			var tokenSource = new CancellationTokenSource();
			tokenSource.CancelAfter(30000);

			while (!tokenSource.IsCancellationRequested)
			{
				var result = await farmer.MainPage.EvaluateFunctionAsync(@"() =>
				{
					if (typeof RewardsCreditRefresh === 'object') {
						let points = RewardsCreditRefresh.GetRewardsHeaderBalance();
						return isNaN(points) ? null : points;
					} else
						return null;
				}");

				if (result != null && result is JToken jResult && jResult.Type == JTokenType.Integer)
					return jResult.ToObject<int>();
				else
					continue;
			}

			return 0;
		}

		public async Task GetCardsAsync()
		{
			farmer.WriteStatus("Gettings cards...");

			ElementHandle element;

			await GoToMicrosoftRewardsPage();

			await farmer.MainPage.WaitForSelectorAsync("div[class=\"points clearfix\"]");

			// Get cards
			var cardsElement = await farmer.MainPage.QuerySelectorAllAsync("span[class=\"mee-icon mee-icon-AddMedium\"]"); // div[class=\"points clearfix\"]

			for (int i = 0; i < cardsElement.Length; i++)
			{
				element = cardsElement[i];
				farmer.WriteStatus($"Card processing {i} / {cardsElement.Length}");

				await CleanMicrosoftRewardsPage();
				if (await element.IsVisible()) // IsIntersectingViewportAsync = bad
				{
					var promise = farmer.Browser.PromiseNewPage(5000);

					try
					{
						await element.ClickAsync();
					}
					catch (PuppeteerException) { continue; } // Not a HTMLElement

					var cardPage = await promise; // This is stupid
					if (cardPage == null) // This method work very well so if the new page is null it's because theire no new farmer.Page.
						continue;

					await cardPage.WaitTillHTMLRendered();
					await farmer.Bing.CheckBingReady();
					await cardPage.WaitTillHTMLRendered();
					await ProceedCard(cardPage);

					promise = farmer.Browser.PromiseNewPage();
					await cardPage.CloseAsync();
					await promise;
				}
			}
		}

		public async Task GoToMicrosoftRewardsPage()
		{
			var url = "https://account.microsoft.com/rewards";

			while (!farmer.MainPage.Url.StartsWith("https://rewards.microsoft.com/")) // Network issue, you know
			{
				/*// If we lost the session
				if (farmer.MainPage.Url.StartsWith("https://login.live.com/"))
				{
					await farmer.Bing.LoginToMicrosoftAsync();
					await farmer.MainPage.WaitForPageToExit("https://login.live.com/"); // Wait for redirection
				}*/

				await farmer.MainPage.TryGoToAsync(url, WaitUntilNavigation.Networkidle0);
				await farmer.MainPage.WaitForPageToExit("https://login.live.com/"); // Wait for redirection

				// Bruteforce this page
				while (farmer.MainPage.Url.StartsWith("https://rewards.microsoft.com/welcome"))
				{
					if (await farmer.MainPage.QuerySelectorAsync("#start-earning-rewards-link") != null) // Sign up (or sign in)
					{
						// Wait the button to be enabled
						await farmer.MainPage.WaitForSelectorToHideAsync("a[id=\"start-earning-rewards-link\"][disabled=\"disabled\"]");

						// If cookies, delete it !
						if (await farmer.MainPage.QuerySelectorAsync("#wcpConsentBannerCtrl") != null)
							await farmer.MainPage.RemoveAsync("#wcpConsentBannerCtrl");

						// Click on the "Sign up to Rewards" link (Name can't be wrong I have the french version so...)
						await farmer.MainPage.ClickAsync("#start-earning-rewards-link");
						await farmer.MainPage.WaitForSelectorToHideAsync("#start-earning-rewards-link", false, 2000);
					}
					else // Weird... (Maybe network, it's always network)
					{
						new Exception("Can't sign in or sign up to Microsoft Rewards");
					}
				}
			}

			// Check if the account has not been ban
			if (await farmer.MainPage.QuerySelectorAsync("#error") != null)
				await GetRawardsError();
		}

		private async Task GetRawardsError()
		{
			var errorJson = await farmer.MainPage.GetInnerTextAsync("h1[class=\"text-headline spacer-32-bottom\"]");

			throw new Exception("Rewards error - " + errorJson);
		}

		private async Task CleanMicrosoftRewardsPage()
		{
			ElementHandle element;

			// If cookies, delete it !
			if (await farmer.MainPage.QuerySelectorAsync("#wcpConsentBannerCtrl") != null)
				await farmer.MainPage.RemoveAsync("#wcpConsentBannerCtrl");

			// Wellcome (75 points it's not worth) // TODO: Remove function
			await farmer.MainPage.RemoveAsync("div[ui-view=\"modalContent\"]");
			await farmer.MainPage.RemoveAsync("div[role=\"presentation\"]");

			// Remove pop up
			while ((element = await farmer.MainPage.QuerySelectorAsync(".mee-icon-Cancel")) != null && await element.IsVisible())
			{
				await element.ClickAsync();
			}
		}

		public async Task ProceedCard(Page cardPage)
		{
			// Wait quest pop off
			ElementHandle element;
			bool succes;

			// Poll quest (Don't support multi quest)
			if ((element = await cardPage.QuerySelectorAsync("div[id^=\"btoption\"]")) != null) // click 
			{
				await farmer.Bing.CheckBingReady();

				// Click on the first option (I wonder why it's always the first option that is the most voted 🤔)

				await element.ClickAsync();
				await cardPage.WaitForSelectorToHideAsync("div[id^=\"btoption\"]");

				return;
			}

			// 50/50 & (quick)Quiz // TODO: make it smart
			succes = false;
			while (!succes && (element = await cardPage.QuerySelectorAsync("input[id=\"rqStartQuiz\"]")) != null)
			{
				await farmer.Bing.CheckBingReady();

				await element.ClickAsync();
				succes = await cardPage.WaitForSelectorToHideAsync("input[id=\"rqStartQuiz\"]", true, 4000);
			}

			// 50/50 & Quiz
			await ProceedQuizCard(cardPage, "div[id^=\"rqAnswerOption\"]");

			// Quick quiz
			await ProceedQuizCard(cardPage, "input[class=\"rqOption\"]");
		}

		private async Task ProceedQuizCard(Page cardPage, string selector)
		{
			ElementHandle element;

			if ((await cardPage.QuerySelectorAsync(selector)) != null)
			{
				while (true)
				{
					if (await cardPage.QuerySelectorAsync("div[class=\"cico rqSumryLogo \"]") != null) break;

					try
					{
						element = await cardPage.WaitForSelectorAsync(selector,
							new WaitForSelectorOptions() { Timeout = 4000 });
					}
					catch (PuppeteerException)
					{
						if (await cardPage.QuerySelectorAsync("div[class=\"cico rqSumryLogo \"]") != null) break;
						await cardPage.ReloadAsync();
						continue;
					}

					if (element == null) continue;

					await farmer.Bing.CheckBingReady();

					while (!await element.IsVisible())
					{
						if (await cardPage.QuerySelectorAsync("div[class=\"cico rqSumryLogo \"]") != null) break;
						await cardPage.WaitForSelectorAsync(selector,
							new WaitForSelectorOptions() { Timeout = 0, Visible = true });
					}

					await element.ClickAsync();
					await cardPage.WaitForSelectorToHideAsync(selector, false, 4000);
				}
			}
		}
		#endregion
	}
}
