# JobScraper — Claude Code Rules

## Project
Blazor Server (.NET 10) app: job scraping, CV management, AI summarization.
Solution: `JobScraper.slnx` | Main project: `JobScraper.Web/` | Unit tests: `JobScraper.Tests/` | Integration tests: `JobScraper.IntegrationTests/`

## Key File Locations
- Entities + EF config: `JobScraper.Web/Common/Entities/*.cs` (entity + `IEntityTypeConfiguration` in same file)
- Features (vertical slices): `JobScraper.Web/Features/{Feature}/`
- Blazor pages: `*.razor` + `*.razor.cs` code-behind pairs
- Module registration: `JobScraper.Web/Features/{Feature}/Setup.cs`
- DbContext: `JobScraper.Web/Modules/Persistence/`
- Program.cs: composition root

## Code Patterns (follow exactly)

### Handler (vertical slice)
```csharp
public class FeatureName
{
    public record Command(...) : IRequest<ErrorOr<Response>>;
    public record Response(...);

    public sealed class Handler(JobsDbContext dbContext) : IRequestHandler<Command, ErrorOr<Response>>
    {
        public async ValueTask<ErrorOr<Response>> Handle(Command command, CancellationToken ct) { }
    }
}
```

### Blazor page code-behind
```csharp
public partial class PageName(IDbContextFactory<JobsDbContext> dbFactory, IMediator mediator) { }
```

### Minimal API endpoint
```csharp
public static class XxxEndpoints
{
    public static IEndpointRouteBuilder MapXxxEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/xxx").RequireAuthorization();
        group.MapGet("/{id:long}", GetById);
        return app;
    }
    private static async Task<IResult> GetById(...) { }
}
```

### Logging
```csharp
public sealed partial class MyClass(...)
{
    [LoggerMessage(LogLevel.Information, "Message {Param}")]
    private static partial void LogSomething(ILogger logger, string Param);
}
```

## Conventions
- C# primary constructors for DI — no field assignments
- `var` everywhere, `sealed` on handlers, file-scoped namespaces
- No `this.` qualification; braces optional for single statements
- `ErrorOr<T>` for failable operations; `Error.Failure(...)` / `Error.Validation(...)`
- Private readonly fields: `_camelCase`; private non-readonly: `camelCase`; constants: `PascalCase`
- 4-space indent, trailing commas in multiline lists
- `IDbContextFactory<JobsDbContext>` in Blazor (never inject DbContext directly)
- Multi-tenancy via `UserJobsContextFactory` — ownership filters applied automatically

## Do NOT
- Inject `JobsDbContext` directly in Blazor pages (use factory)
- Add comments/docstrings to unchanged code
- Create new files when editing an existing one works
- Skip `sealed` on handler classes
- Use field assignments instead of primary constructors

## Testing

### Unit tests (`JobScraper.Tests/`)
- xUnit + Shouldly + NSubstitute
- Direct handler instantiation (no DI container)
- Mirror feature folder structure under `JobScraper.Tests/Features/`
- Naming: `{ClassName}Tests` with descriptive method names

### Integration tests (`JobScraper.IntegrationTests/`)
- Real database via Sqlite in memory + Respawn for cleanup between tests
- `WebApiTestFactory` boots the full app host with `WebApplicationFactory<Program>`
- Inherit from `IntegrationTestBase` — provides `DbContext`, `ObjectMother`, `TimeProviderMock`, `MockHttpMessageHandler`
- Object Mothers in `Factories/` for building test entities (`CompanyObjectMother`, `JobOfferObjectMother`, `UserOfferObjectMother`)
- HTTP mocks via `RichardSzalay.MockHttp` — registered in `Host/HttpMocks/Setup.cs`
- Mirror feature folder structure under `JobScraper.IntegrationTests/Features/`

## Build
- `dotnet build JobScraper.slnx`
- `dotnet test JobScraper.slnx`
- EF migrations: `scripts/add-migration.ps1`
- DB auto-migrates on startup
