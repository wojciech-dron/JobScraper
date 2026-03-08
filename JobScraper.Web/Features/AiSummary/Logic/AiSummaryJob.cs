using System.Diagnostics;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.Cv.Logic;
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

        var config = await dbContext.AiSummaryConfigs
            .Include(x => x.DefaultCv)
            .FirstOrDefaultAsync(cancellationToken);

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
            await ProcessUserOffer(config, offer, cancellationToken);

            if (offer.AiSummaryStatus == AiSummaryStatus.Generated)
                successCount++;
        }

        LogSummaryCompleted(logger, successCount);
    }

    private async Task ProcessUserOffer(AiSummaryConfig config, UserOffer offer, CancellationToken cancellationToken)
    {
        using var offerUrlScope = LogContext.PushProperty("OfferUrl", offer.OfferUrl);
        LogSummaryOfferStart(logger, offer.OfferUrl);

        try
        {
            var cvTemplate = await SelectCvTemplate(config, offer, cancellationToken);

            await SummarizeUserOffer(config, offer, cvTemplate, cancellationToken);

            if (!config.CvGenerationEnabled)
                return;

            if (offer.AiSummaryStatus != AiSummaryStatus.Generated)
                return;

            if (offer.Cv is not null)
                return;

            await CreateDedicatedCvForOffer(config, offer, cvTemplate, cancellationToken);
        }
        catch (Exception)
        {
            offer.AiSummaryStatus = AiSummaryStatus.Error;
        }
        finally
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<CvEntity> SelectCvTemplate(
        AiSummaryConfig config,
        UserOffer offer,
        CancellationToken cancellationToken)
    {
        var request = new SelectCvTemplateForOffer.Request(
            OfferContent: offer.Details.Description ?? "",
            AiModel: config.SmartAiModel            ?? config.DefaultAiModel);

        var result = await mediator.Send(request, cancellationToken);

        var template = await dbContext.Cvs.FirstOrDefaultAsync(t => t.Id == result.CvId, cancellationToken)
         ?? config.DefaultCv;

        ArgumentNullException.ThrowIfNull(template);

        LogSelectedCvTemplate(logger, template.Name);

        return template;
    }

    private async Task SummarizeUserOffer(AiSummaryConfig config,
        UserOffer userOffer,
        CvEntity cvTemplate,
        CancellationToken cancellationToken)
    {
        var request = new SummarizeOfferContent.Request(
            cvTemplate.MarkdownContent,
            userOffer.Details.Description ?? "",
            config.UserRequirements       ?? "",
            config.DefaultAiModel
        );

        var result = await mediator.Send(request, cancellationToken);

        if (result.IsError)
            userOffer.AiSummaryStatus = AiSummaryStatus.Error;
        else
        {
            userOffer.AiSummary = result.Value.AiSummary;
            userOffer.AiSummaryStatus = AiSummaryStatus.Generated;
        }

        LogJobOfferSummaryCompleted(logger, userOffer.OfferUrl, userOffer.AiSummaryStatus);
    }

    private async Task CreateDedicatedCvForOffer(AiSummaryConfig config,
        UserOffer offer,
        CvEntity cvTemplate,
        CancellationToken cancellationToken)
    {
        var request = new AdjustCvForOffer.Request(
            CvContent: cvTemplate.MarkdownContent,
            OfferContent: offer.Details.Description ?? "",
            OfferSummary: offer.AiSummary,
            AiModel: config.SmartAiModel ?? config.DefaultAiModel,
            UserCvRules: config.UserCvRules);

        var result = await mediator.Send(request, cancellationToken);

        if (!result.Success)
            return;

        var newCvName = $"{cvTemplate.Name} - {offer.Details.CompanyName} - {offer.Details.Title}";

        if (await dbContext.Cvs.AnyAsync(x => x.Name == newCvName, cancellationToken))
            newCvName = $"{newCvName} - {DateTime.UtcNow:yyyyMMddHHmm}";

        var duplicateCvRequest = new DuplicateCv.Command(
            OriginCvId: cvTemplate.Id,
            CvName: newCvName,
            NewMarkdownContent: result.AdjustedCvContent!,
            OfferUrl: offer.OfferUrl);

        var dedicatedCvResult = await mediator.Send(duplicateCvRequest, cancellationToken);

        if (!dedicatedCvResult.IsError)
        {
            var dedicatedCv = await dbContext.Cvs.FindAsync([dedicatedCvResult.Value.Id], cancellationToken);

            dedicatedCv!.ChatHistory = result.ChatHistory;
            offer.Cv = dedicatedCv;
        }

        LogCreatedDedicatedCv(logger, dedicatedCvResult.Value.Id);
    }

    [LoggerMessage(LogLevel.Information, "Found {count} offers for summary")]
    static partial void LogFoundCountOffersForSummary(ILogger<ScrapeHandler> logger, int count);

    [LoggerMessage(LogLevel.Information, "AiSummaryJobs completed with {successCount} successfully summaries")]
    static partial void LogSummaryCompleted(ILogger<ScrapeHandler> logger, int successCount);

    [LoggerMessage(LogLevel.Information, "Job offer {jobUrl} summary completed with status {AiSummaryStatus}")]
    static partial void LogJobOfferSummaryCompleted(ILogger<ScrapeHandler> logger,
        string jobUrl,
        AiSummaryStatus? aiSummaryStatus);

    [LoggerMessage(LogLevel.Information, "Summarizing offer {offerUrl}")]
    static partial void LogSummaryOfferStart(ILogger<ScrapeHandler> logger, string offerUrl);
    [LoggerMessage(LogLevel.Information, "Selected CV template {cvName}")]
    static partial void LogSelectedCvTemplate(ILogger<ScrapeHandler> logger, string cvName);
    [LoggerMessage(LogLevel.Information, "Created dedicated CV for offer with id {CvId}")]
    static partial void LogCreatedDedicatedCv(ILogger<ScrapeHandler> logger, long CvId);
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
