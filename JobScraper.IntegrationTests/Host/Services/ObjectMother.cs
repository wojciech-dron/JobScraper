using System.Text;
using JobScraper.Web.Modules.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace JobScraper.IntegrationTests.Host.Services;

public class ObjectMother(
    JobsDbContext dbContext,
    TimeProviderMock timeProvider,
    IServiceProvider serviceProvider)
{
    protected IServiceProvider ServiceProvider { get; } = serviceProvider;
    public JobsDbContext DbContext { get; } = dbContext;
    public TimeProviderMock TimeProvider { get; } = timeProvider;

    public static Random Random { get; } = new(420);

    public static long RandomLong() => Random.NextInt64();
    public static int RandomInt() => Random.Next();

    public static string RandomString(int length = 10)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            var randValue = Random.Next(0, 26);
            var letter = Convert.ToChar(randValue + 65);
            builder.Append(letter);
        }

        return builder.ToString();
    }

    public void Add(object entity) => DbContext.Add(entity);
    public async Task SaveChangesAsync() => await DbContext.SaveChangesAsync();
}

public static class ObjectMotherExtensions
{
    public static Guid GetValueOrRandom(this Guid id) => id == Guid.Empty ? Guid.NewGuid() : id;
    public static int GetValueOrRandom(this int id) => id   == 0 ? ObjectMother.Random.Next() : id;
    public static long GetValueOrRandom(this long id) => id == 0 ? ObjectMother.Random.NextInt64() : id;

    public static long RandomLong(this ObjectMother _) => ObjectMother.RandomLong();
    public static int RandomInt(this ObjectMother _) => ObjectMother.RandomInt();
    public static string RandomString(this ObjectMother _, int length = 10) => ObjectMother.RandomString(length);

    public static IServiceCollection AddObjectMother(this IServiceCollection services)
    {
        services.AddScoped<ObjectMother>();
        return services;
    }
}
