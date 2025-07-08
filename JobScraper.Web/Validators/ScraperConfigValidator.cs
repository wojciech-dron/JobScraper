using FluentValidation;
using JobScraper.Models;
using Microsoft.Extensions.Options;

namespace JobScraper.Web.Validators;

public class ScraperConfigValidator : AbstractValidator<ScraperConfig>
{
    private readonly AppSettings _appSettings;
    public ScraperConfigValidator(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;

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

        RuleForEach(x => x.MyKeywords)
            .NotEmpty()
            .WithMessage("Keyword must not be empty.");

        RuleForEach(x => x.AvoidKeywords)
            .NotEmpty()
            .WithMessage("Avoid keyword must not be empty.");

        RuleFor(x => x.BrowserType)
            .Must(x => _appSettings.AllowedBrowsers.Contains(x))
            .WithMessage("Browser type must be one of the allowed browsers.");
    }
}