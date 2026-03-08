using JobScraper.Web.Common.Entities;
using JobScraper.Web.Modules.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Web.Features.Cv;

public static class CvImageEndpoints
{
    private const long MaxImageBytes = 5 * 1024 * 1024;

    public static IEndpointRouteBuilder MapCvImageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cv-images")
            .RequireAuthorization();

        group.MapPost("/", UploadImage);
        group.MapGet("/{id:long}", GetImage);

        return app;
    }

    private static async Task<IResult> UploadImage(
        HttpContext context,
        [FromForm] IFormFile file,
        [FromServices] JobsDbContext db,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return Results.BadRequest("Cannot upload empty file");

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("Only image files are allowed");

        if (file.Length > MaxImageBytes)
            return Results.BadRequest("Image is too large (max 5 MB)");

        await using var stream = file.OpenReadStream();
        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        var owner = context.User.Identity?.Name;
        var image = new ImageEntity
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            Data = memoryStream.ToArray(),
            Owner = owner,
        };

        db.CvImages.Add(image);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            id = image.Id,
            url = $"/api/cv-images/{image.Id}",
        });
    }

    private static async Task<IResult> GetImage(
        HttpContext context,
        long id,
        [FromServices] JobsDbContext db,
        CancellationToken cancellationToken)
    {
        var image = await db.CvImages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (image is null)
            return Results.NotFound();

        var currentUser = context.User.Identity?.Name;

        if (!string.Equals(image.Owner, currentUser, StringComparison.OrdinalIgnoreCase))
            return Results.Forbid();

        return Results.File(image.Data, image.ContentType, enableRangeProcessing: true);
    }
}
