using ExitSignal;
using MicrosoftRewardsFarmer.TheFarm;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MicrosoftRewardsFarmer
{
	class Program
	{
		#region Variables
		static readonly List<Farmer> farmers = new List<Farmer>();
		static readonly List<Task> tasks = new List<Task>();
		static IExitSignal exitSignal;
#endregion

		#region Properties
		public static Settings Settings { get; private set; }
		#endregion

		#region Methods
		static void Main(string[] args)
		{

			foreach(var arg in args)
				switch (arg)
				{
					case "-h":
					case "-headless":
						AppOptions.Headless = true;
						break;

					case "-ns":
					case "-nosession":
						AppOptions.NoSession = true;
						break;
				}

			Settings = GetSettings();

            SetExitSignal(); // Stop farming when the console close (Close all opened browsers)
			StartFarming();

			Console.WriteLine("Every accounts has finish!");
			Console.WriteLine("Press any key to close");
#if !DEBUG || true
			Console.ReadKey();
#endif
		}

		/*private static void SlowFarm()
		{
			int i = 0;

			foreach (var credentials in Settings.Accounts)
			{
				var farmer = new Farmer(credentials);
				farmer.FarmPoints(i).GetAwaiter().GetResult();
				i++;
			}
		}*/

        private static void StartFarming()
		{
			int i = 0;

			foreach (var credentials in Settings.Accounts)
			{
				var farmer = new Farmer(credentials);
				farmers.Add(farmer);
				tasks.Add(farmer.FarmPoints(i));
				i++;
			}

			Task.WaitAll(tasks.ToArray());
		}

		/*private static void MultiTest()
		{
			var credential = Settings.Accounts[0];
			int n = 10;
			var tasks = new Task[n];
			Task task;
			var stopWatch = new Stopwatch();


			for (int i = 0; i < n; i++)
			{
				var farmerTest = new FarmerTest(credential);

				task = Task.Run(async () =>
					//await farmerTest.RunSearchesTest()
					//await farmerTest.TestGoTo()
					farmerTest.RunRandomWordGenTest(255)
				);
				tasks[i] = task;
			}

			stopWatch.Start();

			Task.WaitAll(tasks);

			stopWatch.Stop();

			var time = new DateTime(stopWatch.ElapsedTicks);
			Console.WriteLine("Test time: " + time.ToString("HH:mm:ss.fff"));
		}*/

		private static void SetExitSignal()
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix)
				exitSignal = new UnixExitSignal();
			else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				exitSignal = new WinExitSignal();

			if (exitSignal != null)
				exitSignal.Exit += ExitSignal_Exit;
		}

        private static async void ExitSignal_Exit(object sender, EventArgs e)
        {
			foreach (var farmer in farmers)
				await farmer.StopAsync();
		}

        private static Settings GetSettings()
		{
			string path = AppPath.GetFullPath(@"\Settings.json");
			var settingsJson = File.ReadAllText(path);
			return JsonConvert.DeserializeObject<Settings>(settingsJson);
		}
#endregion
	}
}
