using JobScraper.IntegrationTests.Factories;
using JobScraper.IntegrationTests.Host;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;
using JobScraper.Web.Features.JobOffers.Scrape.Logic.Custom;
using JobScraper.Web.Modules.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobScraper.IntegrationTests.Features.JobOffers.Scrape.Common;

public class CustomListScraperTests(BaseTestingFixture fixture, ITestOutputHelper outputHelper)
    : IntegrationTestBase(fixture, outputHelper)
{
    private const string TestOrigin = "TestSite";

    /// <summary> Bypasses Playwright by returning pre-built jobs directly from ScrapeJobs. </summary>
    private sealed class TestHandler : ListScraperBaseHandler<CustomListScraper.Command>
    {
        private readonly List<JobOffer> _jobs;

        public TestHandler(
            IOptions<AppSettings> config,
            ILogger<TestHandler> logger,
            JobsDbContext dbContext,
            List<JobOffer> jobs)
            : base(config, logger, dbContext)
        {
            _jobs = jobs;
        }

#pragma warning disable CS1998
        public override async IAsyncEnumerable<List<JobOffer>> ScrapeJobs(SourceConfig sourceConfig)
        {
            yield return _jobs;
        }
#pragma warning restore CS1998
    }

    private TestHandler CreateTestHandler(List<JobOffer> jobs)
    {
        var options = Scope.ServiceProvider.GetRequiredService<IOptions<AppSettings>>();
        var logger = Scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<TestHandler>();
        return new TestHandler(options, logger, DbContext, jobs);
    }

    private CustomListScraper.Handler CreateRealHandler()
    {
        var options = Scope.ServiceProvider.GetRequiredService<IOptions<AppSettings>>();
        var logger = Scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<CustomListScraper.Handler>();
        return new CustomListScraper.Handler(options, logger, DbContext);
    }

    private static CustomListScraper.Command CreateCommand() =>
        new(new SourceConfig { DataOrigin = TestOrigin, SearchUrl = "https://example.com/jobs" });

    [Fact]
    public async Task ShouldReturnEmptyResponse_WhenNoCustomScraperConfigExists()
    {
        ObjectMother.CreateScraperConfig();
        await ObjectMother.SaveChangesAsync();

        var result = await CreateRealHandler().Handle(CreateCommand(), CancellationToken);

        result.ScrapedOffersCount.ShouldBe(0);
        (await DbContext.JobOffers.CountAsync(CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task ShouldSyncJobsAndUserOffersToDatabase()
    {
        ObjectMother.CreateScraperConfig();
        await ObjectMother.SaveChangesAsync();

        var jobs = new List<JobOffer>
        {
            new() { Title = "Backend Dev", OfferUrl = "https://test.com/job/1", Origin = TestOrigin, CompanyName = "Acme", OfferKeywords = [] },
            new() { Title = "Frontend Dev", OfferUrl = "https://test.com/job/2", Origin = TestOrigin, CompanyName = "Acme", OfferKeywords = [] },
        };

        var result = await CreateTestHandler(jobs).Handle(CreateCommand(), CancellationToken);

        result.ScrapedOffersCount.ShouldBe(2);
        (await DbContext.JobOffers.CountAsync(CancellationToken)).ShouldBe(2);
        (await DbContext.UserOffers.CountAsync(CancellationToken)).ShouldBe(2);
    }

    [Fact]
    public async Task ShouldNotCreateDuplicateJobOffers_WhenSameUrlScrapedTwice()
    {
        ObjectMother.CreateScraperConfig();
        await ObjectMother.SaveChangesAsync();

        var jobs = new List<JobOffer>
        {
            new() { Title = "Backend Dev", OfferUrl = "https://test.com/job/1", Origin = TestOrigin, CompanyName = "Acme", OfferKeywords = [] },
        };

        await CreateTestHandler(jobs).Handle(CreateCommand(), CancellationToken);

        ResetServiceScope();

        var secondResult = await CreateTestHandler(jobs).Handle(CreateCommand(), CancellationToken);

        secondResult.ScrapedOffersCount.ShouldBe(0);
        (await DbContext.JobOffers.CountAsync(CancellationToken)).ShouldBe(1);
        (await DbContext.UserOffers.CountAsync(CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task ShouldCreateCompanies_ForDistinctCompanyNamesInScrapedJobs()
    {
        ObjectMother.CreateScraperConfig();
        await ObjectMother.SaveChangesAsync();

        var jobs = new List<JobOffer>
        {
            new() { Title = "Dev",      OfferUrl = "https://test.com/job/1", Origin = TestOrigin, CompanyName = "Acme Corp",  OfferKeywords = [] },
            new() { Title = "Designer", OfferUrl = "https://test.com/job/2", Origin = TestOrigin, CompanyName = "Design Co",  OfferKeywords = [] },
            new() { Title = "QA",       OfferUrl = "https://test.com/job/3", Origin = TestOrigin, CompanyName = "Acme Corp",  OfferKeywords = [] },
        };

        await CreateTestHandler(jobs).Handle(CreateCommand(), CancellationToken);

        var companies = await DbContext.Companies.Select(c => c.Name).ToListAsync(CancellationToken);
        companies.Count.ShouldBe(2);
        companies.ShouldContain("Acme Corp");
        companies.ShouldContain("Design Co");
    }

    [Fact]
    public async Task ShouldTagMatchingKeywords_InUserOfferMyKeywords()
    {
        ObjectMother.CreateScraperConfig(myKeywords: ["C#"]);
        await ObjectMother.SaveChangesAsync();

        var jobs = new List<JobOffer>
        {
            new() { Title = "C# Developer",  OfferUrl = "https://test.com/job/1", Origin = TestOrigin, CompanyName = "Acme", OfferKeywords = [] },
            new() { Title = "Java Developer", OfferUrl = "https://test.com/job/2", Origin = TestOrigin, CompanyName = "Acme", OfferKeywords = [] },
        };

        await CreateTestHandler(jobs).Handle(CreateCommand(), CancellationToken);

        var userOffers = await DbContext.UserOffers
            .Include(u => u.Details)
            .ToListAsync(CancellationToken);

        userOffers.First(u => u.Details.Title == "C# Developer").MyKeywords.ShouldContain("C#");
        userOffers.First(u => u.Details.Title == "Java Developer").MyKeywords.ShouldBeEmpty();
    }
}
