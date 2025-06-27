using FluentValidation;
using JobScraper.Models;

namespace JobScraper.Web.Validators;

public class ScraperConfigValidator : AbstractValidator<ScraperConfig>
{
    public ScraperConfigValidator()
    {
        RuleForEach(x => x.Sources)
            .ChildRules(builder =>
            {
                builder.RuleFor(x => x.DataOrigin)
                    .Must(o => o.IsScrapable())
                    .WithMessage("Data origin must be scrapable.");

                builder.RuleFor(x => x.SearchUrl)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                    .WithMessage("URL is required.")
                    .Matches(@"https?://[^\s]+")
                    .WithMessage("Invalid URL format.");
            });

        RuleForEach(x => x.Keywords)
            .NotEmpty()
            .WithMessage("Keyword must not be empty.");
    }
}