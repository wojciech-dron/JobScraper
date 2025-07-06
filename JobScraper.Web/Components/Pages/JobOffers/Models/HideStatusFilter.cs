using JobScraper.Models;

namespace JobScraper.Web.Components.Pages.JobOffers.Models;

public enum HideStatusFilter
{
    All,
    Hidden,
    Visible,
    Starred,
    Regular
}

public static class HideStatusFilterExtensions
{
    public static IQueryable<JobOffer> ApplyHideStatusFilter(this IQueryable<JobOffer> query,
        HideStatusFilter filter) => filter switch
    {

        HideStatusFilter.Hidden  => query.Where(j => j.HideStatus == HideStatus.Hidden),
        HideStatusFilter.Starred => query.Where(j => j.HideStatus == HideStatus.Starred),
        HideStatusFilter.Regular => query.Where(j => j.HideStatus == HideStatus.Regular),
        HideStatusFilter.Visible => query.Where(j => j.HideStatus == HideStatus.Regular || j.HideStatus == HideStatus.Starred),
        _                        => query,
    };
}