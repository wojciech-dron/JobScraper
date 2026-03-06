using JobScraper.Web.Common.Entities;
using Mediator;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;

public abstract record ScrapeCommand(SourceConfig Source) : IRequest<ScrapeResponse>;

public abstract record ScrapeDetailsCommand(SourceConfig Source) : ScrapeCommand(Source)
{
    public DetailsScrapeStatus[] StatusesToScrape { get; set; } = [DetailsScrapeStatus.ToScrape];
    public string[] OfferUrls { get; set; } = [];
}

public record ScrapeResponse(string[] OffersUrls)
{
    public ScrapeResponse() : this([])
    { }

    public int ScrapedOffersCount => OffersUrls?.Length ?? 0;
}
