namespace JobScraper.Web.Features.Cv.Helpers;

public static class StringExtensions
{
    /// <summary> Removes problematic characters from file name. </summary>
    public static string ToFileName(this string name)
    {
        var invalid = Path.GetInvalidFileNameChars();

        invalid = [.. invalid, ' '];

        return new string([.. name.Where(c => !invalid.Contains(c))]);
    }

    public static string RemoveAiChars(this string str) => str
        .Replace("[DONE]", "")
        .Replace("[FAIL]", "")
        .Replace("–", "-")    // ai long dashes
        .Replace("—", "-")    // ai long dashes 2
        .Replace("‑", "-")    // ai long dashes 3
        .Replace(" ", " ")    // ai spaces
        .Replace("…", "...")  // ai ellipsis
        .Replace("\n---", "") // ai sections
        .Trim();
}
