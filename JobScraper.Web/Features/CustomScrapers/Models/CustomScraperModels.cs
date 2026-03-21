namespace JobScraper.Web.Features.CustomScrapers.Models;

public record CustomJobData(
    string? Title,
    string? Url,
    string? CompanyName,
    string? Location,
    string? Description,
    List<string>? OfferKeywords,
    string? SalaryToParse,
    int? SalaryMinMonth,
    int? SalaryMaxMonth,
    string? SalaryCurrency
);

public record CustomDetailsData(string? Description, List<string>? Keywords);

public record CustomPaginationResult(bool HasNextPage, string? NextPageUrl);
