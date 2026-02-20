namespace JobScraper.Web.Integration.AiProvider;

public class AiProvidersConfig : Dictionary<string, ProviderConfig>
{
    public const string SectionBase = "Integration:AiProviders";

    public const string MainProvider = "Main";

    public string[] AvailableProviders => this.Where(x => x.Value.Visible).Select(x => x.Key).ToArray();
}

public class ProviderConfig
{
    public string ModelId { get; set; } = "arcee-ai/trinity-large-preview:free";
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/";
    public string? ApiKey { get; set; }
    public bool Visible { get; set; }
}
