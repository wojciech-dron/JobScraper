using System.Text.RegularExpressions;
using JobScraper.Logic.Common;
using JobScraper.Models;

namespace JobScraper.Logic.PracujPl;

public partial class SalaryParser
{
    public static void SetSalary(JobOffer jobOffer, string rawSalary)
    {
        // Examples:
        // "22 000 zł netto (+ VAT) / mies."
        // "11 000–16 000 zł netto (+ VAT) / mies."
        // "7 000–10 000 zł / mies. (zal. od umowy)"
        // "130–150 zł netto (+ VAT) / godz."

        if (string.IsNullOrWhiteSpace(rawSalary))
            return;

        // Extract numbers from the salary string
        var numbers = ExtractNumbers(rawSalary);
        if (numbers.Count == 0)
            return;

        int minSalary = numbers[0];
        int? maxSalary = numbers.Count > 1 ? numbers[1] : null;
        string currency = "zł"; // Default currency

        // Determine tax rate
        decimal taxRate = 0m;
        bool isContractDependent = rawSalary.Contains("zal. od umowy");
        bool isBrutto = rawSalary.Contains("brutto");

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

        jobOffer.SalaryCurrency = currency;
    }

    private static List<int> ExtractNumbers(string input)
    {
        var result = new List<int>();

        // First, check if there's a range with '–' character
        if (input.Contains("–"))
        {
            var parts = input.Split('–');
            if (parts.Length >= 2)
            {
                // Extract the first number (before '–')
                var firstNumberStr = ExtractNumberString(parts[0]);
                if (int.TryParse(firstNumberStr, out int firstNumber))
                    result.Add(firstNumber);

                // Extract the second number (after '–')
                var secondNumberStr = ExtractNumberString(parts[1]);
                if (int.TryParse(secondNumberStr, out int secondNumber))
                    result.Add(secondNumber);

                return result;
            }
        }

        // If no range found, extract all numbers
        var matches = System.Text.RegularExpressions.Regex.Matches(input, @"\d+(?:\s\d+)?");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var numberStr = match.Value.Replace(" ", "");
            if (int.TryParse(numberStr, out int number))
                result.Add(number);
        }

        return result;
    }

    private static string ExtractNumberString(string input)
    {
        var match = System.Text.RegularExpressions.Regex.Match(input, @"\d+(?:\s\d+)?");
        return match.Success ? match.Value.Replace(" ", "") : string.Empty;
    }

    [GeneratedRegex(
        @"(?<min>\d+(?:\s\d+)?)\s*(?:–(?<max>\d+(?:\s\d+)?))?\s*(?<currency>[a-zA-Z]+)(?:\s*(?<taxType>brutto|netto)(?:\s*\(\+\s*VAT\))?)?\s*/\s*(?<period>mies\.|godz\.)(?:\s*\(zal\.\s*od\s*umowy\))?",
        RegexOptions.IgnoreCase)]
    private static partial Regex SalaryRegex();
}
