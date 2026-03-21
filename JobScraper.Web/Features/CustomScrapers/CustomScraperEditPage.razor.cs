using BlazorBootstrap;
using BlazorMonaco.Editor;
using JobScraper.Web.Blazor.Extensions;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.CustomScrapers.Logic;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace JobScraper.Web.Features.CustomScrapers;

public sealed partial class CustomScraperEditPage(
    JobsDbContext dbContext,
    IMediator mediator,
    NavigationManager navigationManager,
    IJSRuntime js
) : IAsyncDisposable
{
    private readonly List<ToastMessage> _toasts = [];
    private readonly CancellationTokenSource _cts = new();

    private CustomScraperConfig config = null!;
    private bool isWorking;

    private StandaloneCodeEditor listScriptEditor = null!;
    private StandaloneCodeEditor detailsScriptEditor = null!;
    private StandaloneCodeEditor paginationScriptEditor = null!;

    private string testResult = "";
    private bool isTesting;

    [Parameter]
    public long Id { get; set; }

    private bool IsNew => Id == 0;

    protected override async Task OnInitializedAsync()
    {
        if (IsNew)
            config = new CustomScraperConfig();
        else
            config = await dbContext.CustomScraperConfigs
                    .FirstOrDefaultAsync(c => c.Id == Id, _cts.Token)
             ?? throw new InvalidOperationException($"CustomScraperConfig with ID {Id} not found");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await Global.SetTheme(js, "vs-dark");
    }

    private StandaloneEditorConstructionOptions ListScriptEditorOptions(StandaloneCodeEditor editor) => new()
    {
        AutomaticLayout = true,
        Language = "javascript",
        Theme = "vs-dark",
        Value = config.ListScraperScript,
        FontSize = 13,
        ScrollBeyondLastColumn = 5,
        WordWrap = "on",
    };

    private StandaloneEditorConstructionOptions DetailsScriptEditorOptions(StandaloneCodeEditor editor) => new()
    {
        AutomaticLayout = true,
        Language = "javascript",
        Theme = "vs-dark",
        Value = config.DetailsScraperScript ?? "",
        FontSize = 13,
        ScrollBeyondLastColumn = 5,
        WordWrap = "on",
    };

    private StandaloneEditorConstructionOptions PaginationScriptEditorOptions(StandaloneCodeEditor editor) => new()
    {
        AutomaticLayout = true,
        Language = "javascript",
        Theme = "vs-dark",
        Value = config.PaginationScript ?? "",
        FontSize = 13,
        ScrollBeyondLastColumn = 5,
        WordWrap = "on",
    };

    public async Task SaveAsync()
    {
        config.ListScraperScript = await listScriptEditor.GetValue();
        config.DetailsScraperScript = await detailsScriptEditor.GetValue();
        config.PaginationScript = await paginationScriptEditor.GetValue();

        if (string.IsNullOrWhiteSpace(config.DataOrigin) || string.IsNullOrWhiteSpace(config.Domain))
        {
            _toasts.PushMessage("DataOrigin and Domain are required", ToastType.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(config.ListScraperScript))
        {
            _toasts.PushMessage("List Scraper Script is required", ToastType.Warning);
            return;
        }

        isWorking = true;
        StateHasChanged();

        try
        {
            if (dbContext.Entry(config).State == EntityState.Detached)
                dbContext.Add(config);

            await dbContext.SaveChangesAsync(_cts.Token);
            _toasts.PushMessage("Saved successfully");

            if (IsNew)
                navigationManager.NavigateTo($"custom-scrapers/edit/{config.Id}", true);
        }
        catch (Exception ex)
        {
            _toasts.PushMessage("Failed to save: " + ex.Message, ToastType.Danger);
        }
        finally
        {
            isWorking = false;
        }
    }

    private async Task TestListScriptAsync()
    {
        if (string.IsNullOrWhiteSpace(config.TestListUrl))
        {
            _toasts.PushMessage("Enter a test URL first", ToastType.Warning);
            return;
        }

        var script = await listScriptEditor.GetValue();
        await RunTestAsync(config.TestListUrl, script, TestCustomScript.ScriptType.List);
    }

    private async Task TestDetailsScriptAsync()
    {
        if (string.IsNullOrWhiteSpace(config.TestDetailsUrl))
        {
            _toasts.PushMessage("Enter a test URL first", ToastType.Warning);
            return;
        }

        var script = await detailsScriptEditor.GetValue();
        await RunTestAsync(config.TestDetailsUrl, script, TestCustomScript.ScriptType.Details);
    }

    private async Task TestPaginationScriptAsync()
    {
        if (string.IsNullOrWhiteSpace(config.TestListUrl))
        {
            _toasts.PushMessage("Enter a test URL first", ToastType.Warning);
            return;
        }

        var script = await paginationScriptEditor.GetValue();
        await RunTestAsync(config.TestListUrl, script, TestCustomScript.ScriptType.Pagination);
    }

    private async Task TestFullScenarioAsync()
    {
        if (string.IsNullOrWhiteSpace(config.TestListUrl))
        {
            _toasts.PushMessage("Enter a List/Pagination URL first", ToastType.Warning);
            return;
        }

        var listScript = await listScriptEditor.GetValue();
        if (string.IsNullOrWhiteSpace(listScript))
        {
            _toasts.PushMessage("List script is empty", ToastType.Warning);
            return;
        }

        var paginationScript = await paginationScriptEditor.GetValue();
        var detailsScript = await detailsScriptEditor.GetValue();

        isTesting = true;
        testResult = "";
        StateHasChanged();

        try
        {
            var result = await mediator.Send(
                new TestFullScenario.Command(
                    config.TestListUrl,
                    listScript,
                    paginationScript,
                    detailsScript),
                _cts.Token);

            testResult = result.IsError
                ? $"Error: {result.FirstError.Description}"
                : result.Value.RawResult;
        }
        catch (Exception ex)
        {
            testResult = $"Error: {ex.Message}";
        }
        finally
        {
            isTesting = false;
        }
    }

    private async Task RunTestAsync(string url, string script, TestCustomScript.ScriptType type)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            _toasts.PushMessage("Enter a test URL first", ToastType.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(script))
        {
            _toasts.PushMessage("Script is empty", ToastType.Warning);
            return;
        }

        isTesting = true;
        testResult = "";
        StateHasChanged();

        try
        {
            var result = await mediator.Send(new TestCustomScript.Command(url, script, type), _cts.Token);

            if (result.IsError)
                testResult = $"Error: {result.FirstError.Description}";
            else
                testResult = result.Value.RawResult;
        }
        catch (Exception ex)
        {
            testResult = $"Error: {ex.Message}";
        }
        finally
        {
            isTesting = false;
        }
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext ctx)
    {
        if (isWorking || isTesting)
        {
            var confirm = await js.InvokeAsync<bool>("confirm",
                "Operation in progress. Are you sure you want to leave this page?");
            if (!confirm)
                ctx.PreventNavigation();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
    }
}
