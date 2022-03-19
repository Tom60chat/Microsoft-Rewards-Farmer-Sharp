using Newtonsoft.Json;
using PuppeteerSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MicrosoftRewardsFarmer.TheFarm
{
	public class Session
	{
		#region Constructors
		/// <summary> Create a new instance of Session </summary>
		/// <param name="name"> The session page </param>
		/// <param name="page"> The session name </param>
		/// <exception cref="ArgumentNullException"/>
		public Session(string name, Page page)
		{
			this.name = string.IsNullOrEmpty(name) ?
				throw new ArgumentNullException(nameof(name)) :
				name;
			this.page = page ??
				throw new ArgumentNullException(nameof(page));

			// Check if the session directory is created
			sessionPath = AppPath.GetFullPath(@$"\Sessions\{name}\");
		}
		#endregion

		#region Variables
		private readonly string sessionPath;
		private readonly string name;
		private readonly Page page;
		#endregion

		#region Methods
		public bool Exists() => AppOptions.Session && Directory.Exists(sessionPath);

		/// <summary> Save the current session of the current page </summary>
		public async Task SaveAsync()
		{
			if (!AppOptions.Session) return;

			Directory.CreateDirectory(sessionPath);

			var cookies = await page.GetCookiesAsync();
			var cookiesJson = JsonConvert.SerializeObject(cookies);

			var url = new Uri(page.Url);
			File.WriteAllText(Path.Combine(sessionPath, $"{url.Host}.json"), cookiesJson);

			Debug.WriteLine($"{name} - {url.Host} session saved");
		}

		/// <summary> Restore a saved session of all page </summary>
		/// <returns> If the session was successfully restored </returns>
		public async Task<bool> RestoreAsync()
		{
			if (!AppOptions.Session) return false;

			if (Exists())
			{
				var url = new Uri(page.Url);
				var cokiesFile = Path.Combine(sessionPath, $"{url.Host}.json");

				if (File.Exists(cokiesFile))
				{
					var cookiesJson = File.ReadAllText(cokiesFile);
					var cookies = JsonConvert.DeserializeObject<CookieParam[]>(cookiesJson);

					await page.SetCookieAsync(cookies);

					Debug.WriteLine($"{name} - {url.Host} session restored");

					return true;
				}
			}

			return false;
		}
		#endregion
	}
}
