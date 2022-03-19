namespace MicrosoftRewardsFarmer
{
    public static class AppOptions
	{
		#region Properties
		public static bool Headless { get; private set; } = true;
        public static bool Session { get; private set; } = true;
		#endregion

		#region Methods
		public static void Apply(string[] args)
		{
			foreach (var arg in args)
				switch (arg)
				{
					/*case "-h":
					case "--headless":
						AppOptions.Headless = true;
						break;*/

					case "-nh":
					case "--noheadless":
						Headless = false;
						break;

					case "-ns":
					case "--nosession":
						Session = false;
						break;
				}
		}
		#endregion
	}
}
