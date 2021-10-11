using System;

namespace MicrosoftRewardsFarmer
{
    [Serializable]
    public class Reward
    {
        public string Title { get; set; }
        public uint Cost { get; set; }
        public uint Discounted { get; set; }
    }
}
