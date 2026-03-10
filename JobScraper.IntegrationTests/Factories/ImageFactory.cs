using JobScraper.IntegrationTests.Host.Services;
using JobScraper.Web.Common.Entities;

namespace JobScraper.IntegrationTests.Factories;

public static class ImageFactory
{
    public static ImageEntity CreateImage(this ObjectMother objectMother,
        string fileName = "photo.png",
        string contentType = "image/jpg",
        long size = 1024,
        byte[]? data = null,
        string? owner = "test@email.com")
    {
        var entity = new ImageEntity
        {
            FileName = fileName,
            ContentType = contentType,
            Size = size,
            Data = data ?? [1, 2, 3],
            Owner = owner,
        };

        objectMother.Add(entity);

        return entity;
    }
}
