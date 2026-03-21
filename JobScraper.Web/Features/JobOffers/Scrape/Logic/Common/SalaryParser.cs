using System.Globalization;
using System.Text.RegularExpressions;

namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;

public partial class SalaryParser
{
    public static bool TryParseSalary(string rawSalary,
        out int? salaryMinMonth,
        out int? salaryMaxMonth,
        out string? currency)
    {
        salaryMinMonth = null;
        salaryMaxMonth = null;
        currency = null;

        if (string.IsNullOrWhiteSpace(rawSalary))
            return false;

        if (!HasAnyNumberRegex().IsMatch(rawSalary))
            return false;

        rawSalary = rawSalary
            .Replace("\u00a0", " ")
            .Replace("–", "-")
            .Replace("\u2013", "-")
            .Replace("–", "-")
            .Replace("—", "-")
            .Replace("‑", "-")
            .Replace("&nbsp;", " ")
            .Replace(" ", " ");

        currency = GetCurrency(rawSalary);
        var taxRate = GetTaxRate(rawSalary);
        var period = GetPeriod(rawSalary);

        var numbers = ExtractNumbers(rawSalary);
        if (numbers.Count == 0)
            return false;

        var minSalary = numbers[0];
        decimal? maxSalary = numbers.Count > 1 ? numbers[1] : null;

        salaryMinMonth = minSalary.ApplyMonthPeriod(period).ToNetValue(taxRate);

        salaryMaxMonth = maxSalary.HasValue
            ? maxSalary.Value.ApplyMonthPeriod(period).ToNetValue(taxRate)
            : salaryMinMonth;

        return true;
    }

    private static decimal GetTaxRate(string rawSalary)
    {
        var isContractDependent = rawSalary.Contains("zal. od umowy");
        var isBrutto = rawSalary.Contains("brutto");

        return isBrutto && !isContractDependent ? 0.23m : 0m;
    }

    private static SalaryPeriod GetPeriod(string rawSalary)
    {
        var lower = rawSalary.ToLowerInvariant();

        // Polish
        if (lower.Contains("godz")) return SalaryPeriod.Hour;
        if (lower.Contains("dzień")) return SalaryPeriod.Day;
        if (lower.Contains("tydz")) return SalaryPeriod.Week;
        if (lower.Contains("rok")) return SalaryPeriod.Year;

        // English
        if (lower.Contains("/h")    || lower.Contains("hour")) return SalaryPeriod.Hour;
        if (lower.Contains("/day")  || lower.Contains("a day")) return SalaryPeriod.Day;
        if (lower.Contains("/week") || lower.Contains("a week")) return SalaryPeriod.Week;
        if (lower.Contains("year")) return SalaryPeriod.Year;

        return SalaryPeriod.Month;
    }

    private static List<decimal> ExtractNumbers(string input)
    {
        var result = new List<decimal>();

        input = input.Replace(" ", "");

        if (input.Contains('-'))
        {
            var parts = input.Split('-');
            if (parts.Length >= 2)
            {
                var firstNumberStr = NormalizeNumberString(ExtractNumberString(parts[0]));
                if (decimal.TryParse(firstNumberStr, CultureInfo.InvariantCulture, out var firstNumber))
                    result.Add(firstNumber);

                var secondNumberStr = NormalizeNumberString(ExtractNumberString(parts[1]));
                if (decimal.TryParse(secondNumberStr, CultureInfo.InvariantCulture, out var secondNumber))
                    result.Add(secondNumber);

                return result;
            }
        }

        var matches = SalaryRegex().Matches(input);
        foreach (Match match in matches)
        {
            var numberStr = NormalizeNumberString(match.Value);
            if (decimal.TryParse(numberStr, CultureInfo.InvariantCulture, out var number))
                result.Add(number);
        }

        return result;
    }

    private static string NormalizeNumberString(string numberStr)
    {
        if (string.IsNullOrEmpty(numberStr))
            return numberStr;

        // Comma followed by exactly 3 digits = thousands separator → strip
        if (ThousandsSeparatorRegex().IsMatch(numberStr))
            return numberStr.Replace(",", "");

        // Comma as decimal separator (e.g. "30,50") → replace with dot
        return numberStr.Replace(",", ".");
    }

    private static string ExtractNumberString(string input)
    {
        var match = SalaryRegex().Match(input);

        return match.Success
            ? match.Value
            : string.Empty;
    }

    private static string GetCurrency(string rawSalary)
    {
        if (rawSalary.Contains('$')) return "USD";
        if (rawSalary.Contains('€')) return "EUR";
        if (rawSalary.Contains('£')) return "GBP";
        if (rawSalary.Contains("zł")) return "zł";

        var upperMatch = UppercaseCurrencyRegex().Match(rawSalary);
        if (upperMatch.Success)
            return upperMatch.Value;

        return "PLN";
    }

    [GeneratedRegex(@"\d")]
    private static partial Regex HasAnyNumberRegex();

    [GeneratedRegex(@"\d+[\d,.]*")]
    private static partial Regex SalaryRegex();

    [GeneratedRegex(@"\d{1,3}(,\d{3})+")]
    private static partial Regex ThousandsSeparatorRegex();

    [GeneratedRegex(@"[A-Z]{3}")]
    private static partial Regex UppercaseCurrencyRegex();

}
