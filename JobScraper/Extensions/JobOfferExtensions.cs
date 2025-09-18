using System.Text;
using JobScraper.Models;

namespace JobScraper.Extensions;

public static class JobOfferKeywordExtensions
{
    public static string SetDefaultDescription(this JobOffer jobOffer)
    {
        var stringBuilder = new StringBuilder();

        foreach (var keyword in jobOffer.OfferKeywords)
        {
            stringBuilder.AppendLine(keyword);
        }

        return stringBuilder.ToString();
    }

    public static void ProcessKeywords(this JobOffer jobOffer, ScraperConfig config)
    {
        jobOffer.MyKeywords = config.MyKeywords
            .Where(keyword => ContainKeyword(jobOffer, keyword))
            .ToList();

        if (ShouldStar(jobOffer, config))
            jobOffer.HideStatus = HideStatus.Starred;

        var avoidKeywords = config.AvoidKeywords
            .Where(keyword => ContainKeyword(jobOffer, keyword))
            .ToList();

        jobOffer.MyKeywords.AddRange(avoidKeywords);

        if (avoidKeywords.Count > 0 && jobOffer.HideStatus is not HideStatus.Starred)
            jobOffer.HideStatus = HideStatus.Hidden;
    }

    private static bool ShouldStar(JobOffer jobOffer, ScraperConfig config)
    {
        if (!config.StarMyKeywords)
            return false;

        if (jobOffer.HideStatus is HideStatus.Hidden)
            return false;

        return jobOffer.MyKeywords.Count > 0;
    }

    private static bool ContainKeyword(JobOffer jobOffer, string keyword)
    {
        if (jobOffer.Title!.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            return true;

        if (jobOffer.OfferKeywords.Any(k => k.ToLower() == keyword.ToLower()))
            return true;

        if (jobOffer.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;

        return jobOffer.Location?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true;
    }
}