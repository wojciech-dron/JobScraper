using BlazorBootstrap;
using Blazored.FluentValidation;
using ErrorOr;
using JobScraper.Web.Blazor.Components;
using JobScraper.Web.Blazor.Extensions;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.Cv.Helpers;
using JobScraper.Web.Features.Cv.Logic;
using JobScraper.Web.Features.Cv.PdfGeneration;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace JobScraper.Web.Features.Cv;

public sealed partial class PrepareCvForOfferPage(
    IMediator mediator,
    IJSRuntime js,
    JobsDbContext dbContext,
    NavigationManager navigationManager
) : IAsyncDisposable
{
    private readonly List<ToastMessage> _toasts = [];
    private readonly CancellationTokenSource _cts = new();

    private IJSObjectReference? module;
    private IJSObjectReference? downloadModule;
    private UserOffer offer = null!;
    private CvEntity cvEntity = null!;
    private MarkdownDiffEditor diffEditor = null!;
    private FluentValidationValidator validator = null!;
    private ConfirmDialog dialog = null!;

    private bool isWorking;
    private CompareMode compareMode = CompareMode.WithSaved;
    private string originalContent = "";
    private string modifiedContent = "";

    [Parameter] public string? OfferUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        offer = await dbContext.UserOffers
            .Include(x => x.Details)
            .Include(x => x.Cv!.OriginCv)
            .Include(x => x.Cv!.Image)
            .FirstAsync(x => x.OfferUrl == OfferUrl);

        ArgumentNullException.ThrowIfNull(offer.Cv);
        cvEntity = offer.Cv;
        cvEntity.ChatHistory ??= [];

        originalContent = cvEntity.MarkdownContent;
        modifiedContent = cvEntity.MarkdownContent;
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        module = await js.InvokeAsync<IJSObjectReference>("import", "./Features/Cv/PrepareCvForOfferPage.razor.js");
        await module.InvokeVoidAsync("initPrepareCvResizers");
    }
    private async Task GenerateCvPdf()
    {
        var mdContent = await diffEditor.GetModifiedValueAsync();
        if (string.IsNullOrWhiteSpace(mdContent))
            return;

        isWorking = true;
        StateHasChanged();

        var cvContent = new CvContent(mdContent, cvEntity.Image?.Data, cvEntity.Disclaimer);
        var request = new GenerateCvPdfFromMarkdown.Request(cvContent, cvEntity.LayoutConfig);

        ErrorOr<byte[]> result;
        try
        {
            result = await mediator.Send(request);
        }
        catch (Exception e)
        {
            var errorMessage = e.Message.ToFriendlyErrorMessage();
            _toasts.PushMessage("Failed to Generate PDF PDF: " + errorMessage, ToastType.Danger);
            isWorking = false;
            return;
        }

        isWorking = false;

        if (result.IsError)
        {
            _toasts.PushMessage("Failed to Generate PDF PDF", ToastType.Danger);
            return;
        }

        var fileName = $"{cvEntity.Name}.pdf";
        var pdfBytes = result.Value;
        using var stream = new MemoryStream(pdfBytes);
        using var streamRef = new DotNetStreamReference(stream);

        downloadModule ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/downloadFile.js");
        await downloadModule.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);

        _toasts.PushMessage("CV PDF generated successfully");
    }
    private async Task PrepareCvContent()
    {
        var cvContent = await diffEditor.GetModifiedValueAsync();
        if (string.IsNullOrWhiteSpace(cvContent))
            return;

        if (offer.Details.Description == null)
            return;

        isWorking = true;
        StateHasChanged();
        _toasts.PushMessage("Preparing CV content. It could take a while");

        var request = new AdjustCvForOffer.Request(
            CvContent: cvContent,
            OfferContent: offer.Details.Description,
            OfferSummary: offer.AiSummary);

        var result = await mediator.Send(request, _cts.Token);

        isWorking = false;
        cvEntity.ChatHistory = result.ChatHistory;

        if (!result.Success)
        {
            _toasts.PushMessage("Failed to prepare CV content. Check agents chat history.", ToastType.Warning);
            return;
        }

        _toasts.PushMessage("CV content prepared successfully");

        modifiedContent = result.AdjustedCvContent!;
        _ = diffEditor.SetModifiedModel(modifiedContent);
    }
    private async Task SendChatMessage(string message)
    {
        var cvContent = await diffEditor.GetModifiedValueAsync();
        if (string.IsNullOrWhiteSpace(cvContent) || offer.Details.Description == null)
            return;

        isWorking = true;
        StateHasChanged();

        var request = new AiSimpleCvChatConversation.Request(
            UserMessage: message,
            CurrentCvContent: cvContent,
            OfferContent: offer.Details.Description,
            OfferSummary: offer.AiSummary,
            ExistingChatHistory: cvEntity.ChatHistory);

        var result = await mediator.Send(request, _cts.Token);

        cvEntity.ChatHistory = result.ChatHistory;
        isWorking = false;
    }

    public async Task SaveAsync()
    {
        cvEntity.MarkdownContent = await diffEditor.GetModifiedValueAsync();

        if (!await validator.ValidateAsync())
            return;

        if (dbContext.Entry(cvEntity).State == EntityState.Detached)
            dbContext.Add(cvEntity);

        await dbContext.SaveChangesAsync(_cts.Token);

        if (compareMode == CompareMode.WithSaved)
        {
            originalContent = cvEntity.MarkdownContent;
            await diffEditor.SetOriginalModel(cvEntity.MarkdownContent);
        }

        _toasts.PushMessage("CV saved successfully");
    }
    private async Task DeleteCvAsync()
    {
        var confirmation = await dialog.ShowAsync(
            title: "Are you sure you want to delete this?",
            message1: "This will delete the record. Once deleted can not be rolled back.",
            message2: "Do you want to proceed?");

        if (!confirmation)
            return;

        var cv = await dbContext.Cvs.FindAsync(cvEntity.Id);
        if (cv is null)
            return;

        dbContext.Remove(cv);
        await dbContext.SaveChangesAsync();

        navigationManager.NavigateTo($"/?offerUrl={Uri.EscapeDataString(offer.OfferUrl)}");
    }
    private void GoToCv(long duplicatedCvId) => navigationManager.NavigateTo($"cv/edit/{duplicatedCvId}", true);

    private enum CompareMode
    {
        None,
        WithSaved,
        WithOrigin,
    }

    public bool DisableCompareToOrigin =>
        compareMode is CompareMode.WithOrigin || string.IsNullOrWhiteSpace(cvEntity?.OriginCv?.MarkdownContent);
    private async Task CompareToOrigin()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cvEntity?.OriginCv?.MarkdownContent);
        compareMode = CompareMode.WithOrigin;
        originalContent = cvEntity?.OriginCv?.MarkdownContent!;
        await diffEditor!.SetOriginalModel(cvEntity!.OriginCv!.MarkdownContent);
    }
    public bool DisableCompareToSaved => compareMode is CompareMode.WithSaved;
    private async Task CompareToSaved()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cvEntity?.MarkdownContent);
        compareMode = CompareMode.WithSaved;
        originalContent = cvEntity?.MarkdownContent!;
        await diffEditor!.SetOriginalModel(cvEntity!.MarkdownContent);
    }
    private bool PreventNavigation => isWorking || dbContext.Entry(cvEntity).State == EntityState.Modified;
    private async Task OnBeforeInternalNavigation(LocationChangingContext ctx)
    {
        if (!PreventNavigation)
            return;

        var confirm = await js.InvokeAsync<bool>("confirm", "Are you sure you want to leave this page?");
        if (!confirm)
            ctx.PreventNavigation();
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();

        if (downloadModule is not null)
            await downloadModule.DisposeAsync();
    }
}
