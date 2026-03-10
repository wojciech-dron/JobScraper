using JobScraper.IntegrationTests.Host.Services;
using JobScraper.Web.Common.Entities;

namespace JobScraper.IntegrationTests.Factories;

public static class UserOfferFactory
{
    public static UserOffer CreateUserOffer(this ObjectMother objectMother,
        JobOffer details,
        string? owner = "test@email.com",
        HideStatus hideStatus = HideStatus.Regular,
        List<string>? myKeywords = null,
        string comments = "",
        string? aiSummary = null,
        AiSummaryStatus? aiSummaryStatus = AiSummaryStatus.None)
    {
        var entity = new UserOffer
        {
            Owner = owner,
            OfferUrl = details.OfferUrl,
            HideStatus = hideStatus,
            MyKeywords = myKeywords ?? [],
            Comments = comments,
            AiSummary = aiSummary,
            AiSummaryStatus = aiSummaryStatus,
            Details = details,
        };

        objectMother.Add(entity);

        return entity;
    }
}
