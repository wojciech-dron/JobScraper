using BlazorBootstrap;
using Blazored.FluentValidation;
using Facet;
using Facet.Extensions;
using FluentValidation;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Integration.AiProvider;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Riok.Mapperly.Abstractions;
using TickerQ.Utilities.Enums;

namespace JobScraper.Web.Features.AiSummary;

public partial class AiSummaryConfigPage(
    JobsDbContext dbContext,
    IMediator mediator,
    IJSRuntime js)
{
    private readonly CancellationTokenSource _cts = new();
    private AiSummaryViewModel form = new();

    private bool isWorking;
    private FluentValidationValidator validator = null!;

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrEmpty(dbContext.CurrentUserName))
            throw new InvalidOperationException("User name is not set.");

        form = await dbContext.AiSummaryConfigs
                .Select(AiSummaryViewModel.Projection)
                .FirstOrDefaultAsync()
         ?? new AiSummaryViewModel
            {
                ProviderName = AiProvidersConfig.MainProvider,
                AiSummaryEnabled = true,
            };
    }

    private async Task SaveConfig()
    {
        if (isWorking || !await validator.ValidateAsync())
            return;

        isWorking = true;
        await UpdatePageAsync();

        var dbConfig = await dbContext.AiSummaryConfigs.FirstOrDefaultAsync();

        if (dbConfig is null)
        {
            dbConfig = form.ToSource();
            dbContext.Add(dbConfig);
        }
        else
            dbConfig.ApplyFacet(form);

        await dbContext.SaveChangesAsync();

        isWorking = false;
        PushNotification("Configuration saved successfully.");
    }

    private async Task VerifyProvider()
    {
        isWorking = true;

        var result = await mediator.Send(
            new VerifyProviderAndGetModels.Request(form.ProviderName),
            _cts.Token);

        if (result.IsError)
        {
            PushNotification(result.FirstError.Description, ToastType.Warning);
            isWorking = false;

            return;
        }

        PushNotification("Provider works fine.");
        isWorking = false;
    }

    private async Task SummarizeTestOfferContent()
    {
        if (!await validator.ValidateAsync(options => options.IncludeRuleSets("TestOffer")))
        {
            PushNotification("Provide test offer content.", ToastType.Warning);
            return;
        }

        isWorking = true;
        PushNotification("AI summary in progress...");

        var request = new SummarizeOfferContent.Request(
            CvContent: form.CvContent,
            OfferContent: form.TestOfferContent!,
            UserRequirements: form.UserRequirements ?? "",
            ProviderName: form.ProviderName);

        var result = await mediator.Send(request, _cts.Token);

        if (result.IsError)
            PushNotification(result.FirstError.Description);
        else
            PushNotification("AI summary finished.");

        chatHistory = result.Value.ChatHistory;
        isWorking = false;
    }

    public async Task ScheduleSummaryJob()
    {
        if (isWorking || !await validator.ValidateAsync())
            return;

        await SaveConfig();

        var nextScheduledJob = await dbContext.TimeTickers
            .AsNoTracking()
            .Where(x => x.Function      == AiSummaryJob.FunctionName)
            .Where(x => x.ExecutionTime > DateTime.UtcNow.AddMinutes(-30)) // check if job is scheduled within last 30 minutes
            .Where(x => x.Status == TickerStatus.Idle   ||
                x.Status         == TickerStatus.Queued ||
                x.Status         == TickerStatus.InProgress)
            .OrderByDescending(x => x.ExecutionTime)
            .FirstOrDefaultAsync(_cts.Token);

        var nextExecutionTime = DateTime.UtcNow.AddMinutes(1);
        if (nextScheduledJob is not null && nextScheduledJob.ExecutionTime < nextExecutionTime)
        {
            PushNotification("Ai summary job is already scheduled, and will be executed in the next minute.", ToastType.Warning);
            return;
        }

        isWorking = true;

        dbContext.ScheduleAiSummary(nextExecutionTime);
        await dbContext.SaveChangesAsync(_cts.Token);
        PushNotification("Ai summary job scheduled.");
        isWorking = false;
    }

    private async Task UpdatePageAsync()
    {
        StateHasChanged();
        await Task.Yield();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

[Facet(typeof(AiSummaryConfig),
    exclude: [nameof(AiSummaryConfig.Owner)],
    GenerateToSource = true)]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class AiSummaryViewModel;

public class AiSummaryViewModelValidator : AbstractValidator<AiSummaryViewModel>
{
    private readonly AiProvidersConfig _config;
    public AiSummaryViewModelValidator(IOptions<AiProvidersConfig> config)
    {
        _config = config.Value;

        RuleFor(x => x.ProviderName)
            .NotEmpty()
            .Must(BeAvailable).WithMessage(
                $"Selected provider is not available. Available providers: {string.Join(", ", _config.AvailableProviders)}");

        RuleFor(x => x.CvContent)
            .NotEmpty().WithMessage("CV content is required for AI summary.");

        RuleSet("TestOffer",
            () => RuleFor(x => x.TestOfferContent).NotEmpty());
    }

    private bool BeAvailable(string providerName) => _config.ContainsKey(providerName);
}
