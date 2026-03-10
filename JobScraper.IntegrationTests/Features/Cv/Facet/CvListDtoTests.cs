using JobScraper.IntegrationTests.Factories;
using JobScraper.IntegrationTests.Host;
using JobScraper.Web.Common.Entities;
using JobScraper.Web.Features.Cv.Models;
using Microsoft.EntityFrameworkCore;

namespace JobScraper.IntegrationTests.Features.Cv.Facet;

public class CvListDtoTests(BaseTestingFixture fixture, ITestOutputHelper outputHelper)
    : IntegrationTestBase(fixture, outputHelper)
{
    /// <see cref="CvListDto" />
    [Fact]
    public async Task CvListDto_ShouldProjectFromDb()
    {
        // Arrange
        var originCv = ObjectMother.CreateCv(
            name: "Origin CV",
            isTemplate: true,
            owner: "test@email.com");

        await ObjectMother.SaveChangesAsync();

        var derivedCv = ObjectMother.CreateCv(
            name: "Derived CV",
            isTemplate: false,
            originCv: originCv,
            owner: "test@email.com");

        await ObjectMother.SaveChangesAsync();

        var offer = ObjectMother.CreateJobOffer();
        var userOffer = ObjectMother.CreateUserOffer(offer, hideStatus: HideStatus.Hidden);
        userOffer.Cv = derivedCv;

        await ObjectMother.SaveChangesAsync();

        ResetServiceScope();

        // Act
        var result = await DbContext.Cvs
            .Where(c => c.Id == derivedCv.Id)
            .Select(CvListDto.Projection)
            .FirstAsync(CancellationToken);

        // Assert
        result.Name.ShouldBe("Derived CV");
        result.IsTemplate.ShouldBeFalse();
        result.HasOrigin.ShouldBeTrue();
        result.HasImage.ShouldBeTrue();
        result.OriginName.ShouldBe("Origin CV");
        result.OffersCount.ShouldBe(1);
        result.OfferHidden.ShouldBeTrue();
    }

    /// <see cref="CvListDto" />
    [Fact]
    public async Task CvListDto_WithoutOriginOrImage_ShouldProjectDefaults()
    {
        // Arrange
        ObjectMother.CreateCv(
            name: "Standalone CV",
            isTemplate: true,
            hasImage: false,
            owner: "test@email.com");

        await ObjectMother.SaveChangesAsync();

        ResetServiceScope();

        // Act
        var result = await DbContext.Cvs
            .Where(c => c.Name == "Standalone CV")
            .Select(CvListDto.Projection)
            .FirstAsync(CancellationToken);

        // Assert
        result.Name.ShouldBe("Standalone CV");
        result.IsTemplate.ShouldBeTrue();
        result.HasOrigin.ShouldBeFalse();
        result.HasImage.ShouldBeFalse();
        result.OriginName.ShouldBeNull();
        result.OffersCount.ShouldBe(0);
        result.DerivedCvsCount.ShouldBe(0);
        result.OfferHidden.ShouldBeFalse();
    }
}
