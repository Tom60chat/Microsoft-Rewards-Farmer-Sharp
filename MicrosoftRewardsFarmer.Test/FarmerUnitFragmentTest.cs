using MicrosoftRewardsFarmer.TheFarm;
using Newtonsoft.Json;
using PuppeteerSharp;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MicrosoftRewardsFarmer.Test
{
    public abstract class FarmerUnitFragmentTest : Farmer
    {
        #region Constructors
        public FarmerUnitFragmentTest(ITestOutputHelper output) : base(GetCredentials())
        {
            this.output = output;
        }
        #endregion

        #region Variables
        private readonly ITestOutputHelper output;
        #endregion

        #region Methods
        public static Credentials GetCredentials()
        {
            var settings = Settings.GetSettings();
            if (settings.Accounts == null)
                throw new System.Exception("Missing accounts");
            else
                return settings.Accounts[0];
        }

        private async Task FastLogin()
        {
            var session = new Session(Name, MainPage);
            if (session.Exists())
            {
                if (await Bing.GoToBingAsync())
                {
                    await session.RestoreAsync();

                    if (await MainPage.TryGoToAsync("https://rewards.microsoft.com/", WaitUntilNavigation.Networkidle0))
                        Connected = await session.RestoreAsync();
                }
            }

            if (!Connected)
                await Bing.LoginToMicrosoftAsync();

            output.WriteLine("Logged as " + Name);
        }

        private async Task Init()
        {
            var browserPath = await PuppeteerUtility.GetBrowser();
            await Init(browserPath);
        }

        [Fact]
        public void TestSettings()
        {
            var settings = Settings.GetSettings();
            Assert.NotNull(settings);
        }

        [Fact]
        public async Task TestGoTo()
        {
            int n = 2;
            var tasks = new Task[n];
            Task task;

            var browserPath = await PuppeteerUtility.GetBrowser();
            await Init(browserPath);


            for (int i = 0; i < n; i++)
            {
                var page = await Browser.NewPageAsync();

                task = Task.Run(async () =>
                {
                    int n = 100;

                    for (int i = 0; i < n; i++)
                    {
                        await page.TryGoToAsync("https://bing.com");
                    }
                });
                tasks[i] = task;
            }

            Task.WaitAll(tasks);
        }

        [Fact]
        public async Task TestLogin()
        {
            await Init();
            await Bing.LoginToMicrosoftAsync();
            await MainPage.GoToAsync("https://bing.com");
        }

        [Fact]
        public async Task TestSaveSession()
        {
            await Init();
            await Bing.LoginToMicrosoftAsync();
            var session = new Session(Name, MainPage);

            Assert.True(await Bing.GoToBingAsync());
            await session.SaveAsync();
            Assert.True(await MainPage.TryGoToAsync("https://rewards.microsoft.com/", WaitUntilNavigation.Networkidle0));
            await session.SaveAsync();
        }

        [Fact]
        public async Task TestRestoreSession()
        {
            await Init();
            var session = new Session(Name, MainPage);

            Assert.True(await Bing.GoToBingAsync());
            Assert.True(await session.RestoreAsync());
            Assert.True(await MainPage.TryGoToAsync("https://rewards.microsoft.com/", WaitUntilNavigation.Networkidle0));
            Assert.True(await session.RestoreAsync());
            Assert.True(await MsRewards.GetRewardsPointsAsync() > 0);
        }

        [Fact]
        public async Task TestGetRewardsPoints()
        {
            await Init();
            await FastLogin();
            var points = await MsRewards.GetRewardsPointsAsync();
            output.WriteLine(points.ToString());
        }

        [Fact]
        public async Task TestGetSearchPoints()
        {
            await Init();
            await FastLogin();
            var points = await MsRewards.GetRewardsSearchPoints();
            output.WriteLine(points.ToString());
        }

        [Fact]
        public async Task TestGetCards()
        {
            await Init();
            await FastLogin();
            await MsRewards.GetCardsAsync();

        }

        [Fact]
        public async Task TestProceedCard()
        {
            await Init();
            await Bing.RunSearchesAsync(1);
            // Quiz
            await MainPage.TryGoToAsync(
                "https://www.bing.com/search?q=Nouvelles%20technologies&rnoreward=1&mkt=FR-FR&FORM=ML12JG&skipopalnative=true&rqpiodemo=1&filters=BTEPOKey:%22REWARDSQUIZ_FRFR_MicrosoftRewardsQuizCB_20211130%22%20BTROID:%22Gamification_DailySet_FRFR_20211130_Child2%22%20BTROEC:%220%22%20BTROMC:%2230%22", // Quiz
                WaitUntilNavigation.Networkidle0);
            await MsRewards.ProceedCard(MainPage);
            // Quick quiz
            await MainPage.TryGoToAsync(
                "https://www.bing.com/search?q=qu%27est-ce%20que%20Z%20Event&rnoreward=1&mkt=FR-FR&FORM=ML12JG&skipopalnative=true&rqpiodemo=1&filters=BTEPOKey:%22REWARDSQUIZ_FRFR_MicrosoftRewardsQuizDS_20211201%22%20BTROID:%22Gamification_DailySet_FRFR_20211201_Child2%22%20BTROEC:%220%22%20BTROMC:%2230%22", // Quick Quiz
                WaitUntilNavigation.Networkidle0);
            await MsRewards.ProceedCard(MainPage);
            // 50/50
            await MainPage.TryGoToAsync(
                "https://www.bing.com/search?q=langue%20fran%c3%a7aise&rnoreward=1&mkt=FR-FR&FORM=ML12JG&skipopalnative=true&rqpiodemo=1&filters=BTEPOKey:%22REWARDSQUIZ_FR-FR_ThisOrThat_FrenchLangCountries_EB_20211129%22%20BTROID:%22Gamification_DailySet_FRFR_20211129_Child2%22%20BTROEC:%220%22%20BTROMC:%2250%22%20BTROQN:%220%22", // 50/50
                WaitUntilNavigation.Networkidle0);
            await MsRewards.ProceedCard(MainPage);
            // Poll
            /*await Page.TryGoToAsync(
                "https://www.bing.com/search?q=forets%20en%20france&rnoreward=1&mkt=FR-FR&skipopalnative=true&form=ML17QA&filters=PollScenarioId:%22POLL_FRFR_RewardsDailyPoll_20211207%22%20BTROID:%22Gamification_DailySet_FRFR_20211207_Child3%22%20BTROEC:%220%22%20BTROMC:%2210%22", // Pool
                WaitUntilNavigation.Networkidle0);*/
            await MsRewards.ProceedCard(MainPage);
        }

        [Fact]
        public async Task RunSearchesTest()
        {
            await Init();

            await Bing.RunSearchesAsync(10);

            await StopAsync();
        }

        [Fact]
        public void RunRandomWordGenTest()
        {
            var words = RandomWord.GetWords(10);
            output.WriteLine(string.Join('\n', words));
        }

        [Fact]
        public async Task SwitchTest()
        {
            await Init();
            await Bing.RunSearchesAsync(1);

            await Bing.SwitchToMobileAsync();
            await Task.Delay(250);
            await Bing.SwitchToDesktopAsync();

            await StopAsync();
        }

        /*[Fact]
        public void TestDisplayRedemptionOptions()
        {
            var rand = new Random();

            MsRewards.DisplayRedemptionOptions((uint)rand.Next());
        }*/
        #endregion
    }
}
