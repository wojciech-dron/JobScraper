using FluentValidation;
using JobScraper.Models;
using JobScraper.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.Web.Validators;

public class CreateJobOfferValidator : AbstractValidator<JobOffer>
{
    private readonly IDbContextFactory<JobsDbContext> _dbContextFactory;

    public CreateJobOfferValidator(IDbContextFactory<JobsDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;

        RuleFor(x => x.OfferUrl)
            .NotEmpty().WithMessage("Offer URL is required")
            .MaximumLength(500).WithMessage("Offer URL must not exceed 500 characters")
            .MustAsync(BeUniqueUrl).WithMessage("This offer URL already exists in the database");
            
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters");
            
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(255).WithMessage("Company name must not exceed 255 characters");
            
        RuleFor(x => x.Location)
            .MaximumLength(100).WithMessage("Location must not exceed 100 characters");
            
        RuleFor(x => x.SalaryMinMonth)
            .LessThanOrEqualTo(x => x.SalaryMaxMonth)
            .When(x => x.SalaryMinMonth.HasValue && x.SalaryMaxMonth.HasValue)
            .WithMessage("Minimum salary must be less than or equal to maximum salary");
            
        RuleFor(x => x.SalaryCurrency)
            .MaximumLength(10).WithMessage("Currency must not exceed 10 characters");
            
        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters");
            
        RuleFor(x => x.Comments)
            .MaximumLength(500).WithMessage("Comments must not exceed 500 characters");
    }

    private async Task<bool> BeUniqueUrl(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(url))
            return true; // Let the NotEmpty validation handle this

        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return !await context.JobOffers.AnyAsync(jo => jo.OfferUrl == url, cancellationToken);
    }
}
