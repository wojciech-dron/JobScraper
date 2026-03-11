using System.Text;
using BlazorBootstrap;
using Blazored.FluentValidation;
using ErrorOr;
using JobScraper.Web.Blazor.Components;
using JobScraper.Web.Blazor.Extensions;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.Cv.Components;
using JobScraper.Web.Features.Cv.Helpers;
using JobScraper.Web.Features.Cv.PdfGeneration;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace JobScraper.Web.Features.Cv;

public sealed partial class CvEditPage(
    IJSRuntime js,
    JobsDbContext dbContext,
    IMediator mediator,
    NavigationManager navigationManager
) : IAsyncDisposable
{
    private readonly List<ToastMessage> _toasts = [];
    private FluentValidationValidator validator = null!;
    private DuplicateCvModal duplicateCvModal = null!;
    private IJSObjectReference? downloadModule;
    private readonly CancellationTokenSource _cts = new();
    private MarkdownDiffEditor? diffEditor;
    private ConfirmDialog dialog = null!;
    private bool isWorking;
    private CompareMode compareMode = CompareMode.WithSaved;
    private CvEntity cvEntity = null!;
    private string originalContent = "";
    private string modifiedContent = "";

    [Parameter]
    public int Id { get; set; }

    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static string GetImageUrl(long imageId) => $"/api/cv-images/{imageId}";
    private void GoToCv(long duplicatedCvId) => navigationManager.NavigateTo($"cv/edit/{duplicatedCvId}", true);

    protected override async Task OnInitializedAsync()
    {
        if (Id == 0)
            cvEntity = new CvEntity
            {
                Name = "Test",
            };
        else
            cvEntity = await dbContext.Cvs
                    .Include(c => c.OriginCv)
                    .Include(c => c.Image)
                    .FirstOrDefaultAsync(c => c.Id == Id, _cts.Token)
             ?? throw new InvalidOperationException($"CV with ID {Id} not found");

        compareMode = CompareMode.WithSaved;
        originalContent = cvEntity.MarkdownContent;
        modifiedContent = cvEntity.MarkdownContent;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        ArgumentNullException.ThrowIfNull(diffEditor);
        await diffEditor.SetModels(originalContent, modifiedContent);
    }


    private async Task UploadImage(InputFileChangeEventArgs args)
    {
        var file = args.File;
        if (file.Size == 0)
        {
            _toasts.PushMessage("Cannot upload empty file", ToastType.Warning);
            return;
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            _toasts.PushMessage("Only image files are allowed", ToastType.Warning);
            return;
        }

        if (file.Size > MaxImageBytes)
        {
            _toasts.PushMessage("Image is too large (max 5 MB)", ToastType.Warning);
            return;
        }

        isWorking = true;
        StateHasChanged();

        try
        {
            await using var inputStream = file.OpenReadStream(MaxImageBytes, _cts.Token);
            await using var memoryStream = new MemoryStream();
            await inputStream.CopyToAsync(memoryStream, _cts.Token);

            var image = new ImageEntity
            {
                FileName = file.Name,
                ContentType = file.ContentType,
                Size = file.Size,
                Data = memoryStream.ToArray(),
            };

            dbContext.Add(image);
            await dbContext.SaveChangesAsync(_cts.Token);

            cvEntity.Image = image;
            _toasts.PushMessage("Image uploaded successfully");
        }
        catch (Exception e)
        {
            _toasts.PushMessage("Failed to upload image: " + e.Message, ToastType.Danger);
        }
        finally
        {
            isWorking = false;
        }
    }

    public async Task SaveAsync()
    {
        if (diffEditor is null)
            return;

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
        _hasEditorChanges = false;

        if (Id == 0) // redirect if new cv
            navigationManager.NavigateTo($"cv/edit/{cvEntity.Id}", true);
    }

    private async Task GenerateCvPdf()
    {
        if (diffEditor is null)
            return;

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

    private async Task DownloadMarkdown()
    {
        if (diffEditor is null)
            return;

        var mdContent = await diffEditor.GetModifiedValueAsync();
        if (string.IsNullOrWhiteSpace(mdContent))
            return;

        var fileName = $"{cvEntity.Name}.md";
        var bytes = Encoding.UTF8.GetBytes(mdContent);
        using var stream = new MemoryStream(bytes);
        using var streamRef = new DotNetStreamReference(stream);

        downloadModule ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/downloadFile.js");
        await downloadModule.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
    }

    private async Task DuplicateAsync()
    {
        await SaveAsync();
        await duplicateCvModal.ShowAsync(cvEntity.Id,
            $"{cvEntity.Name} - copy");
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
        navigationManager.NavigateTo("cv");
    }

    private enum CompareMode
    {
        None,
        WithSaved,
        WithOrigin,
    }

    public bool DisableCompareToOrigin => compareMode is CompareMode.WithOrigin ||
        string.IsNullOrWhiteSpace(cvEntity?.OriginCv?.MarkdownContent);
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
    private bool _hasEditorChanges;

    private bool PreventNavigation => isWorking ||
        _hasEditorChanges ||
        dbContext.Entry(cvEntity).State == EntityState.Modified;

    private async Task OnEditorContentChanged()
    {
        if (diffEditor is null)
            return;

        var currentContent = await diffEditor.GetModifiedValueAsync();
        _hasEditorChanges = currentContent != cvEntity.MarkdownContent;
    }

    private async Task<bool> HasUnsavedEditorChanges()
    {
        if (diffEditor is null)
            return false;

        var currentContent = await diffEditor.GetModifiedValueAsync();
        _hasEditorChanges = currentContent != cvEntity.MarkdownContent;
        return _hasEditorChanges;
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext ctx)
    {
        if (!PreventNavigation && !await HasUnsavedEditorChanges())
            return;

        var confirm = await js.InvokeAsync<bool>("confirm", "You have unsaved changes. Are you sure you want to leave this page?");
        if (!confirm)
            ctx.PreventNavigation();
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
    }
}
