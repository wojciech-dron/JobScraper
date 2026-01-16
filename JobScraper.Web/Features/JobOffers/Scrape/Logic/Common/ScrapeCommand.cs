using JobScraper.Web.Common.Entities;
using Mediator;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;

public abstract record ScrapeCommand(SourceConfig Source) : IRequest<ScrapeResponse>;

public record ScrapeResponse(int ScrapedOffersCount = 0)
{
    public ScrapeResponse() : this(0)
    { }
}
