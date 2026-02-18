using JobScraper.Web.Modules.Persistence.Interceptors;

namespace JobScraper.Web.Features.AiSummary;

public class AiProviderConfig : IOwnable
{
    public string? Owner { get; set; } = "system";

    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/";
    public string ModelName { get; set; } = "arcee-ai/trinity-large-preview:free";

    public string? ApiKey { get; set; }

}
