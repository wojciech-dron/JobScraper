using JobScraper.Entities;
using Mediator;

namespace JobScraper.Web.Scraping.Common;

public abstract record ScrapeCommand : IRequest<ScrapeResponse>
{
    public SourceConfig Source { get; init; } = null!;
}

public record ScrapeResponse(int ScrapedOffersCount = 0)
{
    public ScrapeResponse() : this(0)
    { }
}
