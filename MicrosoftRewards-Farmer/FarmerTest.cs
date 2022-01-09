﻿using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace MicrosoftRewardsFarmer
{
    public class FarmerTest : Farmer
    {
        public FarmerTest(Credentials credentials): base(credentials) { }

        public async Task TestGoTo()
        {
            int n = 2;
            var tasks = new Task[n];
            Task task;

            await Init();


            for (int i = 0; i < n; i++)
            {
                var page = await browser.NewPageAsync();

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

        public async Task TestLogin()
        {
            await Init();
            await LoginToMicrosoftAsync();
            await page.GoToAsync("https://bing.com");
        }

        public async Task TestGetRewardsPoints()
        {
            await Init();
            await LoginToMicrosoftAsync();
            var points = await GetRewardsPointsAsync();
            Console.WriteLine(points);
        }

        public async Task TestGetCards()
        {
            await Init();
            await LoginToMicrosoftAsync();
            await GetCardsAsync();

        }

        public async Task TestProceedCard()
        {
            await Init();
            await RunSearchesAsync(1);
            // Quiz
            await page.TryGoToAsync(
                "https://www.bing.com/search?q=Nouvelles%20technologies&rnoreward=1&mkt=FR-FR&FORM=ML12JG&skipopalnative=true&rqpiodemo=1&filters=BTEPOKey:%22REWARDSQUIZ_FRFR_MicrosoftRewardsQuizCB_20211130%22%20BTROID:%22Gamification_DailySet_FRFR_20211130_Child2%22%20BTROEC:%220%22%20BTROMC:%2230%22", // Quiz
                WaitUntilNavigation.Networkidle0);
            await ProceedCard(page);
            // Quick quiz
            await page.TryGoToAsync(
                "https://www.bing.com/search?q=qu%27est-ce%20que%20Z%20Event&rnoreward=1&mkt=FR-FR&FORM=ML12JG&skipopalnative=true&rqpiodemo=1&filters=BTEPOKey:%22REWARDSQUIZ_FRFR_MicrosoftRewardsQuizDS_20211201%22%20BTROID:%22Gamification_DailySet_FRFR_20211201_Child2%22%20BTROEC:%220%22%20BTROMC:%2230%22", // Quick Quiz
                WaitUntilNavigation.Networkidle0);
            await ProceedCard(page);
            // 50/50
            await page.TryGoToAsync(
                "https://www.bing.com/search?q=langue%20fran%c3%a7aise&rnoreward=1&mkt=FR-FR&FORM=ML12JG&skipopalnative=true&rqpiodemo=1&filters=BTEPOKey:%22REWARDSQUIZ_FR-FR_ThisOrThat_FrenchLangCountries_EB_20211129%22%20BTROID:%22Gamification_DailySet_FRFR_20211129_Child2%22%20BTROEC:%220%22%20BTROMC:%2250%22%20BTROQN:%220%22", // 50/50
                WaitUntilNavigation.Networkidle0);
            await ProceedCard(page);
            // Poll
            /*await page.TryGoToAsync(
                "https://www.bing.com/search?q=forets%20en%20france&rnoreward=1&mkt=FR-FR&skipopalnative=true&form=ML17QA&filters=PollScenarioId:%22POLL_FRFR_RewardsDailyPoll_20211207%22%20BTROID:%22Gamification_DailySet_FRFR_20211207_Child3%22%20BTROEC:%220%22%20BTROMC:%2210%22", // Pool
                WaitUntilNavigation.Networkidle0);*/
            await ProceedCard(page);
        }

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
