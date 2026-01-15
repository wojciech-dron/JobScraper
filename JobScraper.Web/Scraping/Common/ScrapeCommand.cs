using JobScraper.Entities;
using Mediator;

namespace JobScraper.Web.Scraping.Common;

public abstract record ScrapeCommand(SourceConfig Source) : IRequest<ScrapeResponse>;

public record ScrapeResponse(int ScrapedOffersCount = 0)
{
    public ScrapeResponse() : this(0)
    { }
}
