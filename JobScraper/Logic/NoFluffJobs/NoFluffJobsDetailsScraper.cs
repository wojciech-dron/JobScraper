using System.Text.Json;
using JobScraper.Logic.Common;
using JobScraper.Models;
using JobScraper.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobScraper.Logic.NoFluffJobs;

public class NoFluffJobsDetailsScraper
{
    public record Command : ScrapeCommand;

    public class Handler : DetailsScrapperBase<Command>
    {

        protected override DataOrigin DataOrigin => DataOrigin.NoFluffJobs;
        public Handler(IOptions<AppSettings> config,
            ILogger<Handler> logger,
            JobsDbContext dbContext)
            : base(config, logger, dbContext)
        { }

        public override async Task<JobOffer> ScrapeJobDetails(JobOffer jobOffer)
        {
            Logger.LogInformation("Scraping {DataOrigin} job details for {OfferUrl}", DataOrigin, jobOffer.OfferUrl);

            var page = await LoadUntilAsync(jobOffer.OfferUrl, waitSeconds: ScrapeConfig.WaitForDetailsSeconds);

            await SaveScreenshot(jobOffer, page);
            await SavePage(jobOffer, page);

            await ScrapeDescription(jobOffer, page);

            return jobOffer;
        }

        private async Task ScrapeDescription(JobOffer jobOffer, IPage page)
        {
            var result = await page.EvaluateAsync<string>(
                """
                () => {
                    const result = {
                        Description: document.querySelector('nfj-read-more')?.textContent.trim(),
                        Keywords: [...document.querySelector('section[commonpostingrequirements]').querySelectorAll('li')].map(x => x?.textContent?.trim()),
                        CompanyUrl: document.querySelector('#postingCompanyUrl').getAttribute('href')
                    }
                    console.log(result)

                    return JSON.stringify(result)
                }
                """);

            var data = JsonSerializer.Deserialize<JobData>(result)!;

            jobOffer.SalaryMinMonth ??= await GetSalaryMinEstimate(page, 20000);

            jobOffer.Description = data.Description;
            jobOffer.OfferKeywords.AddRange(data.Keywords);
            jobOffer.Company!.NoFluffJobsUrl = BaseUrl + data.CompanyUrl;
        }

        /// <param name="checkValue"> Must be dividable by 2000 </param>
        private static async Task<int?> GetSalaryMinEstimate(IPage page, int checkValue) => await page.EvaluateAsync<int?>(
            """
            (async (checkValue) => {
                let salaryComponent = document.querySelector('common-salary-match-inspect');

                // click the salary estimate, using the argument
                salaryComponent?.querySelector(`li.value-${checkValue} > label`)?.click();

                let delayMs = 200;
                await new Promise(resolve => setTimeout(resolve, delayMs));

                let isSuccess = salaryComponent?.querySelector('.tw-text-\\[\\#008000\\]');

                console.log(isSuccess);
                return isSuccess ? checkValue : null;
            });
            """,
            checkValue
        );

        private record JobData(string Description, string CompanyUrl, List<string> Keywords);
    }
}
