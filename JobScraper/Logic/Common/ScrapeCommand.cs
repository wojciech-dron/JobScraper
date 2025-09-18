using JobScraper.Models;
using Mediator;

namespace JobScraper.Logic.Common;

public abstract record ScrapeCommand : IRequest<ScrapeResponse>
{
    public SourceConfig Source { get; init; } = null!;
}

public record ScrapeResponse(int ScrapedOffersCount = 0)
{
    public ScrapeResponse() : this(0)
    { }
}