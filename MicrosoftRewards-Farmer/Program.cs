using ExitSignal;
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
		internal const bool KeepBrowserAlive
#if DEBUG
			= false;
#else
			= false;
#endif
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
			Settings = GetSettings();

            SetExitSignal(); // Stop farming when the console close (Close all opened browsers)
			StartFarming();


			Console.WriteLine("Every accounts has finish!");
			Console.WriteLine("Press any key to close");
			Console.ReadKey();
		}

        private static void StartFarming()
		{
			foreach (var credentials in Settings.Accounts)
			{
				var farmer = new Farmer(credentials);
				farmers.Add(farmer);
				tasks.Add(farmer.FarmPoints());
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

        private static Settings GetSettings()
		{
			var settingsJson = File.ReadAllText("Settings.json");
			return JsonConvert.DeserializeObject<Settings>(settingsJson);
		}
#endregion
	}
}
