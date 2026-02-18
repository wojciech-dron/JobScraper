using JobScraper.Web.Modules.Persistence.Interceptors;

namespace JobScraper.Web.Features.JobOffers.AiSummary;

public class AiSummaryConfig : IOwnable
{
    public string? Owner { get; set; } = "system";

    public string BaseUrl { get; set; } = "https://openrouter.ai/";
    public string ModelName { get; set; } = "arcee-ai/trinity-large-preview:free";
    public string ApiKey { get; set; }

}
