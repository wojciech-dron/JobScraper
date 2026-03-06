using ErrorOr;
using FluentValidation;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Web.Features.Cv.Logic;

public class DuplicateCv
{
    public record Command(
        long OriginCvId,
        string CvName,
        string? OfferUrl = null,
        string NewMarkdownContent = ""
    ) : IRequest<ErrorOr<Response>>;

    public record Response(long Id);

    public class Handler(JobsDbContext dbContext, IValidator<CvEntity> validator)
        : IRequestHandler<Command, ErrorOr<Response>>
    {
        public async ValueTask<ErrorOr<Response>> Handle(Command command, CancellationToken cancellationToken)
        {
            var originCv = await dbContext.Cvs
                .Include(c => c.Image)
                .FirstOrDefaultAsync(c => c.Id == command.OriginCvId, cancellationToken);

            if (originCv is null)
                return Error.Failure(description: "Origin CV not found");

            var newCv = new CvEntity
            {
                Name = command.CvName,
                Image = originCv.Image,
                LayoutConfig = originCv.LayoutConfig.Clone(),
                Disclaimer = originCv.Disclaimer,
                OriginCv = originCv,
            };

            newCv.MarkdownContent = !string.IsNullOrWhiteSpace(command.NewMarkdownContent)
                ? command.NewMarkdownContent
                : originCv.MarkdownContent;

            if (command.OfferUrl is not null)
            {
                var offer = await dbContext.UserOffers
                    .Include(u => u.Details)
                    .FirstOrDefaultAsync(u => u.OfferUrl == command.OfferUrl, cancellationToken);

                offer?.Cv = newCv;
            }


            var validationResult = await validator.ValidateAsync(newCv, cancellationToken);
            if (!validationResult.IsValid)
                return Error.Validation(metadata: validationResult.Errors
                    .Select(x => new KeyValuePair<string, object>(x.PropertyName, x.ErrorMessage))
                    .ToDictionary());

            dbContext.Add(newCv);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response(newCv.Id);
        }
    }
}
