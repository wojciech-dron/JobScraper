using QuestPDF.Infrastructure;

namespace JobScraper.Web.Modules.QuestPdf;

public static class Setup
{
    public static WebApplicationBuilder AddQuestPdf(this WebApplicationBuilder builder)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return builder;
    }

}
