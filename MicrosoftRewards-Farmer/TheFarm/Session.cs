using Newtonsoft.Json;
using PuppeteerSharp;
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
		/// <exception cref="System.ArgumentNullException"/>
		public Session(string name, Page page)
		{
			this.name = string.IsNullOrEmpty(name) ?
				throw new System.ArgumentNullException(nameof(name)) :
				name;
			this.page = page ??
				throw new System.ArgumentNullException(nameof(page));

			// Check if the session directory is created
			var path = AppPath.GetFullPath(@"\Sessions\");
			Directory.CreateDirectory(path);

			sessionPath = Path.Combine(path, name + ".json");
		}
		#endregion

		#region Variables
		private readonly string sessionPath;
		private readonly string name;
		private readonly Page page;
		#endregion


		#region Methods
		public bool Exists() => File.Exists(sessionPath);

		/// <summary> Save the current session </summary>
		public async Task SaveAsync()
		{
			var cookies = await page.GetCookiesAsync();
			var cookiesJson = JsonConvert.SerializeObject(cookies);

			File.WriteAllText(sessionPath, cookiesJson);

			Debug.WriteLine(name + " session saved");
		}

		/// <summary> Restore a saved session </summary>
		/// <returns> If the session was successfully restored </returns>
		public async Task<bool> RestoreAsync()
		{
			if (Exists())
			{
				var cookiesJson = File.ReadAllText(sessionPath);
				var cookies = JsonConvert.DeserializeObject<CookieParam[]>(cookiesJson);

				await page.SetCookieAsync(cookies);

				Debug.WriteLine(name + " session restored");
				return true;
			} 
			else return false;
		}
		#endregion
	}
}
