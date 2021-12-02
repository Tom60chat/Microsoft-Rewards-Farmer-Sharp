using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MicrosoftRewardsFarmer
{
    public class FarmerTest : Farmer
    {
        public FarmerTest(Credentials credentials): base(credentials) { }

        public async Task RunSearchesTest()
        {
            int n = 2;

            await Init();

            for (int i = 0; i < n; i++)
                await RunSearchesAsync(1);

            await StopAsync();
        }

        public async Task SwitchTest()
        {
            int n = 2;

            await Init();
            await RunSearchesAsync(1);

            for (int i = 0; i < n; i++)
            {
                await SwitchToMobileAsync();
                await Task.Delay(250);
                await SwitchToDesktopAsync();
            }

            await StopAsync();
        }

        public void TestDisplayRedemptionOptions()
        {
            int n = 2;
            var rand = new Random();
            var tasks = new Task[n];
            Task task;

            for (int i = 0; i < n; i++)
            {
                task = Task.Run(() =>
                    DisplayRedemptionOptions((uint)rand.Next())
                );
                tasks[i] = task;
            }

            Task.WaitAll(tasks);
        }
    }
}
