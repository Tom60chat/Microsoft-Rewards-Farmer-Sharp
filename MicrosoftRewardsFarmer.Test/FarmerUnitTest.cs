using Xunit.Abstractions;

namespace MicrosoftRewardsFarmer.Test
{
    public class FarmerUnitTest : FarmerUnitFragmentTest
    {
        public FarmerUnitTest(ITestOutputHelper output) : base(output) =>
            AppOptions.Apply(new string[]{ "-nh" });
    }
}
