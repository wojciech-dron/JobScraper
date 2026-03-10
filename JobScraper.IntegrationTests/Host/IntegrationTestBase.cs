using JobScraper.IntegrationTests.Host.Services;
using JobScraper.Web.Modules.Persistence;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace JobScraper.IntegrationTests.Host;
#pragma warning disable CA1051

/// <summary> Integration tests inherit from this to access helper classes </summary>
[Collection<TestingDatabaseFixtureCol>]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly BaseTestingFixture Fixture;
    protected readonly ITestOutputHelper TestOutput;
    protected IServiceScope Scope = null!;

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <summary> Returns a mock handler that controls test host's http clients </summary>
    protected MockHttpMessageHandler MockHttpMessageHandler =>
        Scope.ServiceProvider.GetRequiredService<MockHttpMessageHandler>();

    protected JobsDbContext DbContext => Scope.ServiceProvider.GetRequiredService<JobsDbContext>();
    protected TimeProviderMock TimeProvider => Scope.ServiceProvider.GetRequiredService<TimeProviderMock>();
    protected ObjectMother ObjectMother => Scope.ServiceProvider.GetRequiredService<ObjectMother>();

    protected virtual string CurrentUserName { get; set; } = "test@email.com";

    protected IntegrationTestBase(BaseTestingFixture fixture, ITestOutputHelper outputHelper)
    {
        TestOutput = outputHelper;
        Fixture = fixture;
        Fixture.TestOutput = TestOutput;
    }

    /// <summary> Setup for each test </summary>
    public async ValueTask InitializeAsync()
    {
        await Fixture.TestSetup();
        Scope = Fixture.CreateScope();

        DbContext.CurrentUserName = CurrentUserName;
    }

    public void ResetServiceScope()
    {
        Scope.Dispose();
        Scope = Fixture.CreateScope();

        DbContext.CurrentUserName = CurrentUserName;
    }

    public ValueTask DisposeAsync()
    {
        Scope?.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    protected HttpClient GetAnonymousClient() => Fixture.AnonymousClient.Value;
}
