using System.Text.RegularExpressions;
using JobScraper.Entities;

namespace JobScraper.Web.Scraping.Common;

public partial class SalaryParser
{
    public static bool TryParseSalary(JobOffer jobOffer, string rawSalary)
    {
        // Examples:
        // "22 000 zł netto (+ VAT) / mies."
        // "11 000–16 000 zł netto (+ VAT) / mies."
        // "7 000–10 000 zł / mies. (zal. od umowy)"
        // "130–150 zł netto (+ VAT) / godz."


        if (string.IsNullOrWhiteSpace(rawSalary))
            return false;

        if (!HasAnyNumberRegex().IsMatch(rawSalary))
            return false;

        rawSalary = rawSalary
            .Replace("\u00a0", "")
            .Replace("-", "–");

        // Extract numbers from the salary string
        var numbers = ExtractNumbers(rawSalary);
        if (numbers.Count == 0)
            return false;

        var minSalary = numbers[0];
        decimal? maxSalary = numbers.Count > 1 ? numbers[1] : null;

        // Determine tax rate
        var taxRate = 0m;
        var isContractDependent = rawSalary.Contains("zal. od umowy");
        var isBrutto = rawSalary.Contains("brutto");

        if (isBrutto && !isContractDependent)
            taxRate = 0.23m;

        // Determine period
        var period = SalaryPeriod.Month;
        if (rawSalary.Contains("godz"))
            period = SalaryPeriod.Hour;
        else if (rawSalary.Contains("dzień"))
            period = SalaryPeriod.Day;
        else if (rawSalary.Contains("tydz"))
            period = SalaryPeriod.Week;
        else if (rawSalary.Contains("rok"))
            period = SalaryPeriod.Year;

        // Set salary values
        jobOffer.SalaryMinMonth = minSalary.ApplyMonthPeriod(period).ToNetValue(taxRate);

        if (maxSalary.HasValue)
            jobOffer.SalaryMaxMonth = maxSalary.Value.ApplyMonthPeriod(period).ToNetValue(taxRate);
        else
            jobOffer.SalaryMaxMonth = jobOffer.SalaryMinMonth;

        jobOffer.SalaryCurrency = GetCurrency(rawSalary);

        return true;
    }

    private static List<decimal> ExtractNumbers(string input)
    {
        var result = new List<decimal>();

        input = input.Replace(" ", "");

        // First, check if there's a range with '–' character
        if (input.Contains('-'))
        {
            var parts = input.Split('–');
            if (parts.Length >= 2)
            {
                // Extract the first number (before '–')
                var firstNumberStr = ExtractNumberString(parts[0]);
                if (decimal.TryParse(firstNumberStr, out var firstNumber))
                    result.Add(firstNumber);

                // Extract the second number (after '–')
                var secondNumberStr = ExtractNumberString(parts[1]);
                if (decimal.TryParse(secondNumberStr, out var secondNumber))
                    result.Add(secondNumber);

                return result;
            }
        }

        // If no range found, extract all numbers
        var matches = SalaryRegex().Matches(input);
        foreach (Match match in matches)
        {
            var numberStr = match.Value.Replace(" ", "");
            if (int.TryParse(numberStr, out var number))
                result.Add(number);
        }

        return result;
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
        var match = CurrencyRegex().Match(rawSalary);
        if (!match.Success)
            return "PLN";

        return match.Groups[0].Value;
    }

    [GeneratedRegex(@"\d")]
    private static partial Regex HasAnyNumberRegex();

    [GeneratedRegex(@"\d+[\d,.]*")]
    private static partial Regex SalaryRegex();

    [GeneratedRegex(@"[a-zA-Ząćęłńóśźż]{1,3}")]
    private static partial Regex CurrencyRegex();
}
