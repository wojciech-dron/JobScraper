using System.Text;
using JobScraper.Web.Features.Cv;
using QuestPDF;
using QuestPDF.Infrastructure;
using Shouldly;

namespace JobScraper.Tests.Features.Cv;

public class GenerateCvPdfFromMarkdownTests
{
    public GenerateCvPdfFromMarkdownTests() => Settings.License = LicenseType.Community;

    [Fact]
    public async Task GenerateCvPdf_FromRealMarkdown_SavesToFile()
    {
        // Arrange
        var handler = new GenerateCvPdfFromMarkdown.Handler();

        // Find project root by looking for the .sln file or project directory
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "JobScraper.slnx")))
            currentDir = currentDir.Parent;

        var projectDir = Path.Combine(currentDir!.FullName, "JobScraper.Tests");
        var markdownPath = Path.Combine(projectDir, "Features", "Cv", "CV content.md");
        var outputPath = Path.Combine(projectDir, "Features", "Cv", "CV_output.pdf");

        if (!File.Exists(markdownPath))
        {
            // Fallback for different environments
            markdownPath = Path.Combine(AppContext.BaseDirectory, "Features", "Cv", "CV content.md");
            outputPath = Path.Combine(Path.GetDirectoryName(markdownPath)!, "CV_output.pdf");
        }

        var markdown = await File.ReadAllTextAsync(markdownPath);
        var command = new GenerateCvPdfFromMarkdown.Command(markdown, new LayoutConfig());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldNotBeNull();

        await File.WriteAllBytesAsync(outputPath, result.Value);

        File.Exists(outputPath).ShouldBeTrue();
        var header = Encoding.ASCII.GetString(result.Value.Take(4).ToArray());
        header.ShouldBe("%PDF");
    }
}
