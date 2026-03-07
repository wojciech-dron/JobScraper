using JobScraper.Web.Features.Cv.PdfGeneration;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Web.Features.Cv;

public static class CvPdfEndpoints
{
    public static IEndpointRouteBuilder MapCvPdfEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cv")
            .RequireAuthorization();

        group.MapGet("/{id:long}/pdf", GetPdf);

        return app;
    }

    private static async Task<IResult> GetPdf(
        HttpContext context,
        long id,
        [FromServices] JobsDbContext db,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var cv = await db.Cvs
            .Include(c => c.Image)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cv is null)
            return Results.NotFound();

        var currentUser = context.User.Identity?.Name;

        if (!string.Equals(cv.Owner, currentUser, StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        var content = new CvContent(
            cv.MarkdownContent,
            cv.Image?.Data,
            cv.Disclaimer
        );

        var request = new GenerateCvPdfFromMarkdown.Request(content, cv.LayoutConfig);
        var result = await mediator.Send(request, cancellationToken);

        if (result.IsError)
            return Results.Problem(string.Join("; ", result.Errors.Select(e => e.Description)));

        return Results.File(result.Value, "application/pdf");
    }
}
