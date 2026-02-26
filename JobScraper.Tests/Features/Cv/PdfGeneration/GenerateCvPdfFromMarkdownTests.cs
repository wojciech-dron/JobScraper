using System.Text;
using JobScraper.Web.Features.Cv.PdfGeneration;
using QuestPDF;
using QuestPDF.Infrastructure;
using Shouldly;

namespace JobScraper.Tests.Features.Cv.PdfGeneration;

public class GenerateCvPdfFromMarkdownTests
{
    private const string TestDisclaimer = "Wyrażam zgodę na przetwarzanie moich danych osobowych dla potrzeb niezbędnych " +
        "do realizacji procesu rekrutacji zgodnie z Rozporządzeniem Parlamentu Europejskiego i Rady (UE) 2016/679 "        +
        "z dnia 27 kwietnia 2016 r. w sprawie ochrony osób fizycznych w związku z przetwarzaniem danych osobowych "        +
        "i w sprawie swobodnego przepływu takich danych oraz uchylenia dyrektywy 95/46/WE (RODO).";

    public GenerateCvPdfFromMarkdownTests()
    {
        Settings.EnableDebugging = true;
        Settings.License = LicenseType.Community;
    }

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
        var markdownPath = Path.Combine(projectDir, "Features", "Cv", "PdfGeneration", "CV content.md");
        var outputPath = Path.Combine(projectDir, "Features", "Cv", "PdfGeneration", "CV_output.pdf");
        var imagePath = Path.Combine(projectDir, "Features", "Cv", "PdfGeneration", "image.jpg");

        if (!File.Exists(markdownPath))
        {
            // Fallback for different environments
            markdownPath = Path.Combine(AppContext.BaseDirectory, "Features", "Cv", "CV content.md");
            outputPath = Path.Combine(Path.GetDirectoryName(markdownPath)!, "CV_output.pdf");
            imagePath = Path.Combine(Path.GetDirectoryName(markdownPath)!, "image.jpg");
        }

        var markdown = await File.ReadAllTextAsync(markdownPath);
        var image = await File.ReadAllBytesAsync(imagePath);
        var content = new CvContent(markdown, image, TestDisclaimer);

        var command = new GenerateCvPdfFromMarkdown.Command(content, new LayoutConfig());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.ShouldBeFalse();
        var resultBytes = result.Value;
        result.Value.ShouldNotBeNull();

        await File.WriteAllBytesAsync(outputPath, resultBytes);

        File.Exists(outputPath).ShouldBeTrue();
        var header = Encoding.ASCII.GetString([.. resultBytes.Take(4)]);
        header.ShouldBe("%PDF");
    }
}
