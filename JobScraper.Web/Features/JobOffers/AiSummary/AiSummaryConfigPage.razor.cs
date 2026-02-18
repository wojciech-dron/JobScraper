using Blazored.FluentValidation;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace JobScraper.Web.Features.JobOffers.AiSummary;

public partial class AiSummaryConfigPage
{
    private readonly IDbContextFactory<JobsDbContext> _dbFactory;
    private readonly IJSRuntime _js;
    private readonly AiSummaryConfig _config = new();
    private readonly JobsDbContext _dbContext = null!;
    private readonly IMediator _mediator;
    private readonly CancellationTokenSource _cts = new();

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

        // config = await dbContext.ScraperConfigs.FirstOrDefaultAsync() ?? new ScraperConfig();

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

    private async Task VerifyConnection()
    {
        var result = await _mediator.Send(new VerifyConnection.Request(_config), _cts.Token);


        if (result.IsError)
        {
            PushNotification(result.FirstError.Description);
            return;
        }

        PushNotification("Connection verified successfully.");
        Console.WriteLine(result.Value.Data);
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
