using FluentValidation;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Modules.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Web.Features.Cv.Validators;

public class CvEntityValidator : AbstractValidator<CvEntity>
{
    private readonly JobsDbContext _dbContext;
    private const int MaxNameLength = 255;
    private const int MinContentLength = 20;
    private const int MaxContentLength = 30000;
    private const int MaxDisclaimerLength = 1000;

    public CvEntityValidator(JobsDbContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name cannot be empty")
            .MaximumLength(MaxNameLength).WithMessage($"Name must not exceed {MaxNameLength} characters")
            .MustAsync(BeUnique).WithMessage("Name already exists");

        RuleFor(x => x.MarkdownContent)
            .NotEmpty().WithMessage("Content cannot be empty")
            .MinimumLength(MinContentLength).WithMessage($"Content must be at least {MinContentLength} characters")
            .MaximumLength(MaxContentLength).WithMessage($"Content must not exceed {MaxContentLength} characters");

        RuleFor(x => x.Disclaimer)
            .MaximumLength(MaxDisclaimerLength).WithMessage($"Disclaimer must not exceed {MaxDisclaimerLength} characters");
    }

    private async Task<bool> BeUnique(CvEntity cv, string name, CancellationToken cancellationToken)
    {
        return !await _dbContext.Cvs
            .Where(c => c.Id != cv.Id)
            .AnyAsync(c => c.Name == name, cancellationToken);
    }
}
