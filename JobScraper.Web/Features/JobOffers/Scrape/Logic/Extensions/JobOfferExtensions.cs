using System.Text;
using JobScraper.Web.Common.Entities;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Extensions;

public static class JobOfferExtensions
{
    public static string SetDefaultDescription(this JobOffer jobOffer)
    {
        var stringBuilder = new StringBuilder();

        foreach (var keyword in jobOffer.OfferKeywords)
            stringBuilder.AppendLine(keyword);

        return stringBuilder.ToString();
    }

    public static UserOffer ProcessKeywords(this UserOffer userOffer, ScraperConfig config)
    {
        userOffer.MyKeywords = config.MyKeywords
            .Where(keyword => ContainKeyword(userOffer.Details, keyword))
            .ToList();

        if (ShouldStar(userOffer, config))
            userOffer.HideStatus = HideStatus.Starred;

        var avoidKeywords = config.AvoidKeywords
            .Where(keyword => ContainKeyword(userOffer.Details, keyword))
            .ToList();

        userOffer.MyKeywords.AddRange(avoidKeywords);

        if (ShouldHide(userOffer, avoidKeywords))
            userOffer.HideStatus = HideStatus.Hidden;

        return userOffer;
    }

    private static bool ShouldStar(UserOffer offer, ScraperConfig config)
    {
        if (!config.StarMyKeywords)
            return false;

        if (offer.HideStatus is not HideStatus.Regular)
            return false;

        return offer.MyKeywords.Count > 0;
    }

    private static bool ShouldHide(UserOffer offer, List<string> avoidKeywords)
    {
        if (offer.HideStatus is not HideStatus.Regular)
            return false;

        if (avoidKeywords.Count == 0)
            return false;

        return true;
    }

    private static bool ContainKeyword(JobOffer jobOffer, string keyword)
    {
        if (jobOffer.Title!.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            return true;

#pragma warning disable CA1862
        if (jobOffer.OfferKeywords.Any(k => k.ToLower() == keyword.ToLower()))
            return true;

        if (jobOffer.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
            return true;

        return jobOffer.Location?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true;
    }
}
