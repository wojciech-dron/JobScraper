using Blazored.FluentValidation;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.JSInterop;

namespace JobScraper.Web.Features.JobOffers.AiSummary;

public partial class AiSummaryConfigPage
{
    private readonly IJSRuntime _js;
    private readonly JobsDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly CancellationTokenSource _cts = new();
    private readonly AiSummaryConfig _config = new();

    private bool isWorking;
    private FluentValidationValidator validator = null!;

    public AiSummaryConfigPage(JobsDbContext dbContext,
        IMediator mediator,
        IJSRuntime js)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _js = js;
    }

    protected override Task OnInitializedAsync()
    {
        if (string.IsNullOrEmpty(_dbContext.CurrentUserName))
            throw new InvalidOperationException("User name is not set.");

        return Task.CompletedTask;

    }

    private async Task SaveConfig()
    {
        if (isWorking || !await validator.ValidateAsync())
            return;

        isWorking = true;
        await UpdatePageAsync();

        if (_config.Owner == "system") // check if it is a default value
            _dbContext.Add(_config);
        else
            _dbContext.Update(_config);

        await _dbContext.SaveChangesAsync();

        isWorking = false;
        PushNotification("Configuration saved successfully.");
    }

    private async Task VerifyModel()
    {
        var result = await _mediator.Send(new GetAvailableModels.Request(_config), _cts.Token);

        if (result.IsError)
        {
            PushNotification(result.FirstError.Description);
            return;
        }

        var models = result.Value.Models;
        var modelsCount = models.Length;

        if (models.Any(m => m.Id == _config.ModelName))
            PushNotification($"Model verified. {modelsCount} models found.");
        else
            PushNotification($"Connection verified successfully, but specified model does not exist. " +
                $"{modelsCount} models found.");

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
