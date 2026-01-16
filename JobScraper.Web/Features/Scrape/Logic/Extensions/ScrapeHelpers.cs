namespace JobScraper.Web.Features.Scrape.Logic.Extensions;

public class ScrapeHelpers
{
    public static async Task<string> GetJsScript(string resourceName)
    {
        var assembly = typeof(ScrapeHelpers).Assembly;
        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Embedded resource {resourceName} not found");

        using var reader = new StreamReader(stream);
        var script = await reader.ReadToEndAsync();
        return script;
    }
}
