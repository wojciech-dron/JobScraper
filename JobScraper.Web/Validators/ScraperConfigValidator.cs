using System.Text.RegularExpressions;
using FluentValidation;
using JobScraper.Models;
using Microsoft.Extensions.Options;
using NCrontab;

namespace JobScraper.Web.Validators;

public partial class ScraperConfigValidator : AbstractValidator<ScraperConfig>
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

        RuleFor(x => x.ScrapeCron)
            .Must(x => CronRegex().IsMatch(x))
            .When(x => !string.IsNullOrEmpty(x.ScrapeCron))
            .WithMessage("Cron expression is invalid.");
    }

    [GeneratedRegex(@"^((((\d+,)+\d+|(\d+(\/|-|#)\d+)|\d+L?|\*(\/\d+)?|L(-\d+)?|\?|[A-Z]{3}(-[A-Z]{3})?) ?){5,7})|(@(annually|yearly|monthly|weekly|daily|hourly|reboot))|(@every (\d+(ns|us|µs|ms|s|m|h))+)$")]
    private static partial Regex CronRegex();
}