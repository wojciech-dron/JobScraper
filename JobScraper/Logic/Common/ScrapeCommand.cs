using JobScraper.Models;
using MediatR;

namespace JobScraper.Logic.Common;

public record ScrapeCommand : IRequest
{
    public SourceConfig Source { get; init; } = null!;
}