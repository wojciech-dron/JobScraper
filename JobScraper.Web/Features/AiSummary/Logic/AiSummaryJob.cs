using System.Diagnostics;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.JobOffers.Scrape;
using JobScraper.Web.Modules.Persistence;
using JobScraper.Web.Modules.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;
using TickerQ.Utilities;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Entities;

namespace JobScraper.Web.Features.AiSummary.Logic;

public record struct SummaryRequest(string Owner);

public sealed partial class AiSummaryJob(
    IMediator mediator,
    UserProvider userProvider,
    JobsDbContext dbContext,
    ILogger<ScrapeHandler> logger)
{
    public const string FunctionName = "AiSummaryJobs";

    [TickerFunction(FunctionName)]
    public async Task AiSummaryJobs(TickerFunctionContext<SummaryRequest> context, CancellationToken cancellationToken)
    {
        using var activity = new Activity(FunctionName).Start();

        dbContext.CurrentUserName = context.Request.Owner;
        userProvider.UserName = context.Request.Owner;

        using var userNameScope = LogContext.PushProperty("UserName", userProvider.UserName);

        var config = await dbContext.AiSummaryConfigs.FirstOrDefaultAsync(cancellationToken);
        if (config is not { AiSummaryEnabled: true })
            return;

        var offersForSummary = await dbContext.UserOffers
            .Include(x => x.Details)
            .Where(x => x.AiSummaryStatus == AiSummaryStatus.Marked)
            .ToArrayAsync(cancellationToken);

        LogFoundCountOffersForSummary(logger, offersForSummary.Length);

        var successCount = 0;
        foreach (var offer in offersForSummary)
        {
            await SummarizeUserOffer(config, offer, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (offer.AiSummaryStatus == AiSummaryStatus.Generated)
                successCount++;

            LogJobOfferSummaryCompleted(logger, offer.OfferUrl);
        }

        LogSummaryCompleted(logger, successCount);
    }

    private async Task SummarizeUserOffer(AiSummaryConfig config,
        UserOffer userOffer,
        CancellationToken cancellationToken)
    {
        using var offerUrlScope = LogContext.PushProperty("OfferUrl", userOffer.OfferUrl);
        LogSummaryOfferStart(logger, userOffer.OfferUrl);

        var arguments = new SummarizeOfferContent.Request(
            config.CvContent,
            userOffer.Details.Description ?? "",
            config.UserRequirements       ?? "",
            config.ProviderName
        );

        try
        {
            var result = await mediator.Send(arguments, cancellationToken);

            if (result.IsError)
                userOffer.AiSummaryStatus = AiSummaryStatus.Error;
            else
            {
                userOffer.AiSummary = result.Value.AiSummary;
                userOffer.AiSummaryStatus = AiSummaryStatus.Generated;
            }
        }
        catch (Exception)
        {
            userOffer.AiSummaryStatus = AiSummaryStatus.Error;
        }
    }

    [LoggerMessage(LogLevel.Information, "Found {count} offers for summary")]
    static partial void LogFoundCountOffersForSummary(ILogger<ScrapeHandler> logger, int count);

    [LoggerMessage(LogLevel.Information, "AiSummaryJobs completed with {successCount} successfully summaries")]
    static partial void LogSummaryCompleted(ILogger<ScrapeHandler> logger, int successCount);

    [LoggerMessage(LogLevel.Information, "Job offer {JobUrl} summary completed")]
    static partial void LogJobOfferSummaryCompleted(ILogger<ScrapeHandler> logger, string JobUrl);
    [LoggerMessage(LogLevel.Information, "Summarizing offer {OfferUrl}")]
    static partial void LogSummaryOfferStart(ILogger<ScrapeHandler> logger, string OfferUrl);
}

public static class AiSummaryJobExtensions
{
    /// <summary> Adds AiSummaryJob to the schedule, requires SaveChanges to be called </summary>
    public static void ScheduleAiSummary(this JobsDbContext dbContext, DateTime executionTime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbContext.CurrentUserName);

        var request = new SummaryRequest(dbContext.CurrentUserName);

        var cronTickerEntity = new TimeTickerEntity
        {
            ExecutionTime = executionTime, // this is always stored as utc
            Function = AiSummaryJob.FunctionName,
            Description = "Summarize offers with ai",
            Request = TickerHelper.CreateTickerRequest(request),
            Retries = 1,
            RetryIntervals = [20], // set in seconds
        };

        dbContext.Add(cronTickerEntity);
    }
}
