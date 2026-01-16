using JobScraper.Web.Common.Entities;

namespace JobScraper.Web.Features.JobOffers.Models;

public enum HideStatusFilter
{
    All,
    Visible,
    Hidden,
    Starred,
    Regular,
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
