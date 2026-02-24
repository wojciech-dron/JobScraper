using JobScraper.Web.Features.Cv.PdfGeneration;
using JobScraper.Web.Modules.Persistence.Interceptors;

namespace JobScraper.Web.Common.Entities;

public class CvData : IUpdatable, IOwnable
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? Owner { get; set; }

    public string MarkdownContent { get; set; } = "";
    public LayoutConfig LayoutConfig { get; set; } = new();
    public string Disclaimer { get; set; } = "";

    public string? PdfPath { get; set; }

    public CvData? OriginCv { get; set; }
}
