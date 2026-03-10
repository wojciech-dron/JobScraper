using JobScraper.IntegrationTests.Factories;
using JobScraper.IntegrationTests.Host;
using JobScraper.Web.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.IntegrationTests.Features.Test;

public class ProjectableEfTests(BaseTestingFixture fixture, ITestOutputHelper outputHelper)
    : IntegrationTestBase(fixture, outputHelper)
{
    [Fact]
    public async Task ProjectableUserOffer_ShouldReturn_FirstUserOffer()
    {
        // Arrange
        var offer = ObjectMother.CreateJobOffer(
            offerUrl: "https://example.com/job/projectable-test");

        var userOffer = ObjectMother.CreateUserOffer(offer);

        await ObjectMother.SaveChangesAsync();

        ResetServiceScope();

        // Act
        var result = await DbContext.JobOffers
            .Where(j => j.OfferUrl == offer.OfferUrl)
            .Select(j => j.UserOffer)
            .FirstAsync(CancellationToken);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ProjectableUserOffer_ShouldReturnNull_WhenNoUserOfferExists()
    {
        // Arrange
        var offer = ObjectMother.CreateJobOffer(
            offerUrl: "https://example.com/job/no-user-offer");

        await ObjectMother.SaveChangesAsync();

        ResetServiceScope();

        // Act
        var result = await DbContext.JobOffers
            .Where(j => j.OfferUrl == offer.OfferUrl)
            .Select(j => j.UserOffer)
            .FirstOrDefaultAsync(CancellationToken);

        // Assert
        result.Should().BeNull();
    }
}
