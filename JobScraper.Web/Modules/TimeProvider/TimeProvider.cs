namespace JobScraper.Web.Modules.TimeProvider;

public interface ITimeProvider
{
    public DateTime UtcNow { get; }
    public DateTimeOffset UtcOffsetNow { get; }
}

public class TimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateTimeOffset UtcOffsetNow => DateTimeOffset.UtcNow;
}

public static class Setup
{
    public static IServiceCollection AddTimeProvider(this IServiceCollection services)
    {
        services.AddScoped<ITimeProvider, TimeProvider>();

        return services;
    }
}
