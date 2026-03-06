using BlazorBootstrap;
using Blazored.FluentValidation;
using Facet;
using FluentValidation;
using JobScraper.Web.Blazor.Extensions;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Common.Models;
using JobScraper.Web.Features.AiSummary.Logic;
using JobScraper.Web.Integration.AiProvider;
using JobScraper.Web.Modules.Auth;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using TickerQ.Utilities.Enums;

namespace JobScraper.Web.Features.AiSummary;

public sealed partial class AiSummaryConfigPage(
    JobsDbContext dbContext,
    IMediator mediator,
    IJSRuntime js,
    IOptions<AiProvidersConfig> config
) : IAsyncDisposable
{
    [CascadingParameter] private Task<AuthenticationState> AuthStateTask { get; set; } = default!;

    private readonly CancellationTokenSource _cts = new();
    private readonly List<ToastMessage> toasts = [];
    private AiSummaryViewModel form = new();
    private List<CvViewModel> availableCvs = [];
    private string[] aiModels = [];
    private CvViewModel? selectedCv;
    private List<ChatItem> chatHistory = [];
    private FluentValidationValidator validator = null!;
    private bool isWorking;

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrEmpty(dbContext.CurrentUserName))
            throw new InvalidOperationException("User name is not set.");

        form = await dbContext.AiSummaryConfigs
                .Select(AiSummaryViewModel.Projection)
                .FirstOrDefaultAsync()
         ?? new AiSummaryViewModel
            {
                Owner = "default",
                DefaultAiModel = AiProvidersConfig.MainProvider,
                AiSummaryEnabled = true,
            };

        await LoadAvailableModels();
        await LoadCvCandidatesAsync();
    }
    private async Task LoadAvailableModels()
    {
        var authState = await AuthStateTask;
        var isAdmin = authState.User.IsInRole(AppRoles.Admin);

        aiModels = isAdmin
            ? config.Value.AllProviders
            : config.Value.VisibleProviders;
    }

    private async Task LoadCvCandidatesAsync()
    {
        availableCvs = await dbContext.Cvs
            .AsNoTracking()
            .Where(cv => cv.IsTemplate)
            .OrderBy(c => c.Id)
            .Select(CvViewModel.Projection)
            .ToListAsync();

        selectedCv = availableCvs.FirstOrDefault(x => x.Id == form.DefaultCv?.Id);
    }

    private async Task SaveConfig()
    {
        if (isWorking || !await validator.ValidateAsync())
            return;

        isWorking = true;
        await UpdatePageAsync();

        var dbConfig = form.ToSource();
        dbConfig.DefaultCv = await dbContext.Cvs
            .FirstOrDefaultAsync(cv => cv.Id == form.DefaultCv!.Id);

        if (dbConfig.Owner == "default")
            dbContext.Add(dbConfig);
        else
            dbContext.Update(dbConfig);

        await dbContext.SaveChangesAsync();

        isWorking = false;
        toasts.PushMessage("Configuration saved successfully.");
    }

    private async Task VerifyAiModel(string? aiModel)
    {
        if (string.IsNullOrEmpty(aiModel))
            return;

        isWorking = true;

        var result = await mediator.Send(
            new VerifyProviderAndGetModels.Request(aiModel),
            _cts.Token);

        if (result.IsError)
        {
            toasts.PushMessage(result.FirstError.Description, ToastType.Warning);
            isWorking = false;

            return;
        }

        toasts.PushMessage("Model connection is OK.");
        isWorking = false;
    }

    private async Task SummarizeTestOfferContent()
    {
        if (!await validator.ValidateAsync(options => options.IncludeRuleSets("TestOffer")))
        {
            toasts.PushMessage("Provide test offer content.", ToastType.Warning);
            return;
        }

        isWorking = true;
        toasts.PushMessage("AI summary in progress...");

        var aiModel = !string.IsNullOrEmpty(form.SmartAiModel) ? form.SmartAiModel : form.DefaultAiModel;
        var request = new SummarizeOfferContent.Request(
            CvContent: form.CvContent,
            OfferContent: form.TestOfferContent!,
            UserRequirementsForOffer: form.UserRequirements ?? "",
            ProviderName: aiModel);

        var result = await mediator.Send(request, _cts.Token);

        if (result.IsError)
            toasts.PushMessage(result.FirstError.Description);
        else
            toasts.PushMessage("AI summary finished.");

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
            toasts.PushMessage("Ai summary job is already scheduled, and will be executed in the next minute.", ToastType.Warning);
            return;
        }

        isWorking = true;

        dbContext.ScheduleAiSummary(nextExecutionTime);
        await dbContext.SaveChangesAsync(_cts.Token);
        toasts.PushMessage("Ai summary job scheduled.");
        isWorking = false;
    }

    private async Task UpdatePageAsync()
    {
        StateHasChanged();
        await Task.Yield();
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext ctx)
    {
        if (!isWorking)
            return;

        var confirm = await js.InvokeAsync<bool>("confirm", "Are you sure you want to leave this page?");
        if (!confirm)
            ctx.PreventNavigation();
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
    }

    [Facet(typeof(AiSummaryConfig),
        GenerateToSource = true,
        NestedFacets = [typeof(CvViewModel)])]
    public partial class AiSummaryViewModel;

    [Facet(typeof(CvEntity),
        Include = [nameof(CvEntity.Id), nameof(CvEntity.Name)],
        GenerateToSource = true
    )]
    public partial class CvViewModel;

    public class AiSummaryViewModelValidator : AbstractValidator<AiSummaryViewModel>
    {
        private readonly AiProvidersConfig _config;

        public AiSummaryViewModelValidator(IOptions<AiProvidersConfig> config)
        {
            _config = config.Value;
            RuleLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.DefaultAiModel)
                .NotEmpty()
                .Must(BeAvailable).WithMessage(
                    $"Selected provider is not available. Available providers: {string.Join(", ", _config.VisibleProviders)}");

            RuleFor(x => x.SmartAiModel)
                .Must(x => string.IsNullOrWhiteSpace(x) || BeAvailable(x))
                .WithMessage(
                    $"Selected provider is not available. Available providers: {string.Join(", ", _config.VisibleProviders)}");

            RuleFor(x => x.DefaultCv)
                .NotNull().WithMessage("Default CV cannot be empty");

            RuleSet("TestOffer",
                () => RuleFor(x => x.TestOfferContent).NotEmpty());
        }

        private bool BeAvailable(string providerName) => _config.ContainsKey(providerName);
    }
}
