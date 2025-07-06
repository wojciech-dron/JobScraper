using JobScraper.Models;
using MediatR;

namespace JobScraper.Logic.Common;

public record ScrapeCommand : IRequest<ScrapeResponse>
{
    public SourceConfig Source { get; init; } = null!;
}

public record ScrapeResponse(int ScrapedOffersCount = 0)
{
    public ScrapeResponse() : this(0)
    {
        ScrapedOffersCount = 0;
    }
}