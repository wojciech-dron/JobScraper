using JobScraper.Web.Modules.TimeProvider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JobScraper.IntegrationTests.Host.Services;

/// <summary> Scoped time provider with current time setting </summary>
public class TimeProviderMock : ITimeProvider
{
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;
    public DateTimeOffset UtcOffsetNow { get; set; } = DateTimeOffset.UtcNow;

    public void Advance(TimeSpan timeSpan)
    {
        UtcNow += timeSpan;
        UtcOffsetNow += timeSpan;
    }
}

public static class TimeProviderMockSetup
{
    public static IServiceCollection AddTimeProviderMock(this IServiceCollection services)
    {
        services.AddScoped<TimeProviderMock>();
        services.Replace(new ServiceDescriptor(typeof(ITimeProvider),
            sp => sp.GetRequiredService<TimeProviderMock>(),
            ServiceLifetime.Scoped));

        return services;
    }
}
