namespace JobScraper.Web.Extensions;

public class BoolFilterHelper
{
    public static readonly bool?[] Options = [null, true, false];

    public static string AppliedStringSelector(bool? value) => value switch
    {
        null => "All",
        true => "Applied",
        false => "Non applied"
    };

    public static string HiddenStringSelector(bool? value) => value switch
    {
        null => "All",
        true => "Hidden",
        false => "Visible"
    };
}

