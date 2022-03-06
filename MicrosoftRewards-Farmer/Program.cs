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

			Settings = Settings.GetSettings();

            SetExitSignal(); // Stop farming when the console close (Close all opened browsers)

			if (Settings == null || Settings.Accounts == null || Settings.Accounts.Length == 0)
			{
				Console.WriteLine("No accounts found");
			}
			else
			{
				StartFarming();

				Console.WriteLine();
				Console.WriteLine("Every accounts has finish!");
			}
			Console.WriteLine();
			Console.WriteLine("Press any key to exit.");
			Console.ReadKey();
		}

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
#endregion
	}
}
