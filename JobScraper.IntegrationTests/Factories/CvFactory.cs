using JobScraper.IntegrationTests.Host.Services;
using JobScraper.Web.Common.Entities;

namespace JobScraper.IntegrationTests.Factories;

public static class CvFactory
{
    public static CvEntity CreateCv(this ObjectMother objectMother,
        string name = "Test CV",
        bool isTemplate = false,
        string markdownContent = "# Test CV Content",
        string disclaimer = "",
        string? owner = "test@email.com",
        CvEntity? originCv = null,
        ImageEntity? image = null,
        bool hasImage = true)
    {
        if (hasImage)
            image ??= objectMother.CreateImage();

        var entity = new CvEntity
        {
            Name = name,
            IsTemplate = isTemplate,
            MarkdownContent = markdownContent,
            Disclaimer = disclaimer,
            Owner = owner,
            OriginCv = originCv,
            Image = image,
        };

        objectMother.Add(entity);

        return entity;
    }
}
