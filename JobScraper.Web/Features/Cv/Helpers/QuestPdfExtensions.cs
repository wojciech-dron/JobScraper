namespace JobScraper.Web.Features.Cv.Helpers;

public static class QuestPdfExtensions
{
    private const string MessageToReplace = "To further investigate the location of the root cause, "                             +
        "please run the application with a debugger attached or set the QuestPDF.Settings.EnableDebugging flag to true. " +
        "The library will generate additional debugging information such as "                                             +
        "probable code problem location and detailed layout measurement overview.";

    private const string FriendlyMessage = "Try to modify size and content of elements (for example headers).";

    public static string ToFriendlyErrorMessage(this string errorMessage)
    {
        return errorMessage.Replace(MessageToReplace, FriendlyMessage);
    }
}
