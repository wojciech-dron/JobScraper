namespace JobScraper.Web.Features.JobOffers.Scrape.Logic.Common;

public enum SalaryPeriod
{
    Hour = 12 * 20 * 8, // 8 hours per day
    Day = 12  * 20,     // 20 days per month
    Week = 12 * 4,
    Month = 12,
    Year = 1,
}

public static class SalaryExtensions
{
    public static int ApplyMonthPeriod(this int amount, SalaryPeriod period) =>
        amount * (int)period / 12;

    public static decimal ApplyMonthPeriod(this decimal amount, SalaryPeriod period) =>
        amount * (decimal)period / 12;

    public static int ToNetValue(this int grossSalary, decimal taxRate) =>
        (int)(grossSalary / (taxRate + 1));

    public static int ToNetValue(this decimal grossSalary, decimal taxRate) =>
        (int)(grossSalary / (taxRate + 1));
}
