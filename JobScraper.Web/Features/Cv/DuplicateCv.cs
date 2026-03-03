using ErrorOr;
using FluentValidation;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Web.Features.Cv;

public class DuplicateCv
{
    public record Request(
        long OriginCvId,
        string CvName,
        string? OfferUrl = null
        ) : IRequest<ErrorOr<Response>>;

    public record Response(long Id);

    public class Handler(JobsDbContext dbContext, IValidator<CvEntity> validator)
        : IRequestHandler<Request, ErrorOr<Response>>
    {
        public async ValueTask<ErrorOr<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var originCv = await dbContext.Cvs
                .Include(c => c.Image)
                .FirstOrDefaultAsync(c => c.Id == request.OriginCvId, cancellationToken);

            if (originCv is null)
                return Error.Failure(description: "Origin CV not found");

            var newCv = new CvEntity
            {
                Name = request.CvName,
                Image = originCv.Image,
                MarkdownContent = originCv.MarkdownContent,
                LayoutConfig = originCv.LayoutConfig.Clone(),
                Disclaimer = originCv.Disclaimer,
                OriginCv = originCv,
            };

            var validationResult = await validator.ValidateAsync(newCv, cancellationToken);
            if (!validationResult.IsValid)
                return Error.Validation(metadata: validationResult.Errors
                    .Select(x => new KeyValuePair<string, object>(x.PropertyName, x.ErrorMessage))
                    .ToDictionary());

            if (request.OfferUrl is not null)
            {
                var offer = await dbContext.UserOffers
                    .Include(u => u.Details)
                    .FirstOrDefaultAsync(u => u.OfferUrl == request.OfferUrl, cancellationToken);

                offer?.Cv = newCv;
            }

            dbContext.Add(newCv);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response(newCv.Id);
        }
    }
}
