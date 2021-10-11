using PuppeteerSharp;
using System;

namespace MicrosoftRewardsFarmer
{
    [Serializable]
    public class Settings
    {
        public Credentials[] Accounts { get; set; }
        public Reward[] Rewards { get; set; }
    }
}
