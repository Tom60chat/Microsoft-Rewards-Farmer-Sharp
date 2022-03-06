using PuppeteerSharp;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MicrosoftRewardsFarmer.TheFarm
{
	public class Farmer
	{
		#region Constructors
		private Farmer() { }

		public Farmer(Credentials credentials)
		{
			if (credentials == null)
			{
				WriteStatus("[Exception] Farmer - No account found");
				farming = true; // It's cheating
			}
			else
			{
				Credentials = credentials;

				if (credentials.Username == null)
				{
					WriteStatus("[Exception] Farmer - No username found");
					farming = true; // It's cheating
				}
				else if (credentials.Username == "")
				{
					WriteStatus("[Exception] Farmer - Empty username");
					farming = true; // It's cheating
				}
				else
					Name = Credentials.Username.Substring(0, Credentials.Username.IndexOf('@'));

				if (credentials.Password == null)
				{
					WriteStatus("[Exception] Farmer - No password found");
					farming = true; // It's cheating
				}
				else if (AppOptions.Headless && credentials.Password == "")
				{
					WriteStatus("[Exception] Farmer - Empty password");
					farming = true; // It's cheating
				}
			}

			Bing = new Bing(this);
			MsRewards = new MsRewards(this);
		}
		#endregion

		#region Variables
		public readonly string Name;

		private int consoleTop;
		private bool farming;
		private int userPoints;
		private byte progress;
		private const byte totalProgress = 8;

		public bool Mobile;
		public bool Connected;
		public readonly string DefaultUserAgent =
			"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.82 Safari/537.36 Edg/93.0.961.52";
		#endregion

		#region Properties
		public ViewPortOptions DefaultViewport { get; private set; }
		public Browser Browser { get; private set; }
		public Page MainPage { get; private set; }
		public Credentials Credentials { get; protected set; }
		public Bing Bing { get; private set; }
		public MsRewards MsRewards { get; private set; }
		#endregion

		#region Methods
		protected async Task Init(int consoleTop = 0)
		{
			this.consoleTop = consoleTop;
			Browser = await PuppeteerUtility.GetBrowser(AppOptions.Headless);

			try
			{
				// Init
				MainPage = await Browser.GetCurrentPage();
				DefaultViewport = MainPage.Viewport;

				await MainPage.SetUserAgentAsync(DefaultUserAgent);
#if DEBUG
				MainPage.SetConsoleOutput();
#endif
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

#if !DEBUG || true
			try
			{
#endif
				// LogIn
				var session = new Session(Name, MainPage);
				if (session.Exists())
				{
					WriteStatus("Restoring session...");

					if (await Bing.GoToBingAsync())
					{
						await session.RestoreAsync();

						if (await MainPage.TryGoToAsync("https://rewards.microsoft.com/", WaitUntilNavigation.Networkidle0))
							Connected = await session.RestoreAsync();
					}
				}

				if (!Connected)
					await Bing.LoginToMicrosoftAsync();
				progress++;

				// Get account points
				userPoints = await MsRewards.GetRewardsPointsAsync();
				progress++;

				// Resolve cards
				await MsRewards.GetCardsAsync();
				progress++;

				// Saving MsRewards session
				await session.SaveAsync();
				progress++;

				// Run seaches
				await Bing.RunSearchesAsync((90 / 3) + 4); // 90 points max / 3 points per page
				progress++;

				await Bing.SwitchToMobileAsync();
				await Bing.RunSearchesAsync(60 / 3); // 60 points max / 3 points per page
				progress++;

				// Saving Bing session
				await session.SaveAsync();
				progress++;

				// Get end points
				var endRewardPoints = await MsRewards.GetRewardsPointsAsync();
				progress++;

				if (userPoints <= 0)
					WriteStatus($"Done - <$Yellow;Total: {endRewardPoints}>");
				else
					WriteStatus($"Done - Gain: {endRewardPoints - userPoints} - <$Yellow;Total: {endRewardPoints}>");
#if !DEBUG || true
			}
			catch (Exception e)
			{
				Debug.WriteLine(e); // Send full error to debugger console
				WriteStatus("[Exception] FarmPoints: " + e.Message);
			}
			finally
			{
#endif
				await Browser.CloseAsync();
				await Browser.DisposeAsync();
				farming = false;
#if !DEBUG || true
			}
#endif
		}

		public async Task StopAsync()
		{
			if (Browser != null)
			{
				farming = false;
				await Browser.CloseAsync();
				await Browser.DisposeAsync();
			}
		}

		internal void WriteStatus(string status)
        {
			string points = $"{userPoints} point";
			string value =
				$"<$Gray;[><$Green;{Name}><$Gray;](><$Cyan;{progress}/{totalProgress}><$Gray;) - ><$Blue;{points}><$Gray;: >{status}";

			DynamicConsole.ClearLine(consoleTop);
			DynamicConsole.CustomAction(
						() => ColoredConsole.WriteLine(value),
						0, consoleTop);

#if DEBUG
			Debug.WriteLine($"[{Name}]({progress}/{totalProgress}) - {points}: {status}");
#endif
		}
		#endregion
	}
}
