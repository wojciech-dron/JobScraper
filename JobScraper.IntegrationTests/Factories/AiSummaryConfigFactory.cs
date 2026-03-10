using JobScraper.IntegrationTests.Host.Services;
using JobScraper.Web.Common.Entities;

namespace JobScraper.IntegrationTests.Factories;

public static class AiSummaryConfigFactory
{
    public static AiSummaryConfig CreateAiSummaryConfig(this ObjectMother objectMother,
        string owner = "test@email.com",
        bool aiSummaryEnabled = true,
        bool cvGenerationEnabled = false,
        string defaultAiModel = "test-model",
        string? smartAiModel = null,
        string cvContent = "",
        string? userRequirements = null,
        string? userCvRules = null,
        string? testOfferContent = null,
        CvEntity? defaultCv = null)
    {
        var entity = new AiSummaryConfig
        {
            Owner = owner,
            AiSummaryEnabled = aiSummaryEnabled,
            CvGenerationEnabled = cvGenerationEnabled,
            DefaultAiModel = defaultAiModel,
            SmartAiModel = smartAiModel,
            CvContent = cvContent,
            UserRequirements = userRequirements,
            UserCvRules = userCvRules,
            TestOfferContent = testOfferContent,
            DefaultCv = defaultCv,
        };

        objectMother.Add(entity);

        return entity;
    }
}
