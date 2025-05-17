namespace JobScraper.Logic.Common;

public enum SalaryPeriod
{
    Hour = 12 * 30 * 24,
    Day = 12  * 30,
    Week = 12 * 4,
    Month = 12,
    Year = 1,
}

public static class SalaryExtensions
{
    public static int ApplyMonthPeriod(this int amount,  SalaryPeriod period) =>
        amount * (int)period / 12;

    public static int ToNetValue(this int grossSalary, decimal taxRate) =>
        (int)(grossSalary / (taxRate + 1));
}