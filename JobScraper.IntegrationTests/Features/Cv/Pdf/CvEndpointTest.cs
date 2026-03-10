using System.Text;
using JobScraper.IntegrationTests.Factories;
using JobScraper.IntegrationTests.Host;

namespace JobScraper.IntegrationTests.Features.Cv.Pdf;

public class CvEndpointTest(BaseTestingFixture fixture, ITestOutputHelper outputHelper)
    : IntegrationTestBase(fixture, outputHelper)
{
    [Fact]
    public async Task GetCvEndpoint_ShouldReturnOk()
    {
        // arrange
        var projectDir = GetProjectDir();
        var imageData = await File.ReadAllBytesAsync(
            Path.Combine(projectDir, "Features", "Cv", "Pdf", "image.jpg"),
            CancellationToken);

        var image = ObjectMother.CreateImage(data: imageData);
        var cv = ObjectMother.CreateCv(image: image);
        await ObjectMother.SaveChangesAsync();

        ResetServiceScope();

        var client = GetAuthenticatedClient();

        // act
        var response = await client.GetAsync($"cv/{cv.Id}/pdf", CancellationToken);

        // assert
        response.EnsureSuccessStatusCode();
        var resultBytes = await response.Content.ReadAsByteArrayAsync(CancellationToken);

        var outputPath = Path.Combine(projectDir, "Features", "Cv", "Pdf", "output.pdf");
        await File.WriteAllBytesAsync(outputPath, resultBytes, CancellationToken);
        File.Exists(outputPath).ShouldBeTrue();
        var header = Encoding.ASCII.GetString([.. resultBytes.Take(4)]);
        header.ShouldBe("%PDF");
    }

    private static string GetProjectDir()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "JobScraper.slnx")))
            currentDir = currentDir.Parent;

        var projectDir = Path.Combine(currentDir!.FullName, "JobScraper.IntegrationTests");

        return projectDir;
    }
}
