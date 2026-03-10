using JobScraper.IntegrationTests.Factories;
using JobScraper.IntegrationTests.Host;
using JobScraper.Web.Features.AiSummary;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.IntegrationTests.Features.AiSummary.Facet;

public class AiSummaryViewModelTests(BaseTestingFixture fixture, ITestOutputHelper outputHelper)
    : IntegrationTestBase(fixture, outputHelper)
{
    /// <see cref="AiSummaryConfigPage.AiSummaryViewModel" />
    [Fact]
    public async Task AiSummaryViewModel_ShouldProjectFromDb()
    {
        // Arrange
        var cv = ObjectMother.CreateCv(name: "Template CV", isTemplate: true);
        await ObjectMother.SaveChangesAsync();

        var config = ObjectMother.CreateAiSummaryConfig(
            aiSummaryEnabled: true,
            cvGenerationEnabled: true,
            defaultAiModel: "gpt-4",
            smartAiModel: "claude-sonnet",
            userRequirements: "Remote only",
            userCvRules: "Keep it short",
            testOfferContent: "Test offer content",
            defaultCv: cv);

        await ObjectMother.SaveChangesAsync();

        ResetServiceScope();

        // Act
        var result = await DbContext.AiSummaryConfigs
            .Select(AiSummaryConfigPage.AiSummaryViewModel.Projection)
            .FirstAsync(CancellationToken);

        // Assert
        result.AiSummaryEnabled.ShouldBeTrue();
        result.CvGenerationEnabled.ShouldBeTrue();
        result.DefaultAiModel.ShouldBe("gpt-4");
        result.SmartAiModel.ShouldBe("claude-sonnet");
        result.UserRequirements.ShouldBe("Remote only");
        result.UserCvRules.ShouldBe("Keep it short");
        result.TestOfferContent.ShouldBe("Test offer content");
        result.DefaultCv.ShouldNotBeNull();
        result.DefaultCv!.Name.ShouldBe("Template CV");
    }
}
