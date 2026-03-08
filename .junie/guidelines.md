# JobScraper2 — Solution Guidelines

## Solution Overview

JobScraper2 is a Blazor Server (.NET 10) application for scraping job offers from Polish job portals, managing CVs, and using AI to summarize offers and adjust CVs. It uses SQLite for persistence and runs scheduled scraping jobs.

### Projects

| Project | Purpose |
|---|---|
| `JobScraper.Web` | Main web app (Blazor SSR, API endpoints, all business logic) |
| `JobScraper.Tests` | Unit tests (`InternalsVisibleTo` from Web project) |

### Key Technologies & Libraries

- **Framework**: .NET 10, ASP.NET Core, Blazor Server (SSR)
- **UI**: Blazor Bootstrap, BlazorMonaco, QuickGrid
- **CQRS/Mediator**: `Mediator` (source-generated, scoped lifetime)
- **Persistence**: EF Core + SQLite, pooled `DbContextFactory`, ownership query filters
- **Validation**: FluentValidation + Blazored.FluentValidation
- **Error handling**: `ErrorOr` for result types
- **Mapping**: Riok.Mapperly (source-generated)
- **AI**: Microsoft Semantic Kernel (agents, OpenAI connectors)
- **Scraping**: Microsoft Playwright (browser automation)
- **PDF**: QuestPDF
- **Scheduling**: TickerQ (cron/time-based jobs)
- **Messaging**: WolverineFx
- **Logging**: Serilog (with OpenTelemetry, structured logging)
- **Resilience**: Polly
- **DI scanning**: Scrutor
- **Testing**: xUnit, Shouldly, NSubstitute, coverlet

## Architecture

### Feature-Based Folder Structure (Vertical Slices)

```
JobScraper.Web/
├── Features/           # Business features (vertical slices)
│   ├── Account/        # Auth pages
│   ├── AiSummary/      # AI offer summarization
│   ├── Cv/             # CV management, PDF generation
│   ├── JobOffers/      # Job offers, scraping, companies
│   └── Users/
├── Modules/            # Cross-cutting infrastructure
│   ├── Auth/
│   ├── Extensions/
│   ├── Jobs/           # TickerQ scheduling setup
│   ├── Logging/
│   ├── Mediator/       # Mediator config + behaviours
│   ├── Persistence/    # DbContext, migrations, interceptors
│   ├── QuestPdf/
│   ├── Security/
│   ├── Services/
│   ├── Settings/
│   └── Wolverine/      # Wolverine middlewares + setup
├── Blazor/             # Shared Blazor components, layout
├── Common/             # Shared entities, models, extensions
│   ├── Entities/
│   ├── Extensions/
│   └── Models/
├── Integration/        # External service integrations (AI providers)
├── Validators/         # Shared FluentValidation validators
└── Program.cs          # Composition root
```

### Module Registration Pattern

Each module/feature has a `Setup.cs` with extension methods:
- **Services**: `builder.AddXxx()` on `WebApplicationBuilder`
- **Middleware/Endpoints**: `app.UseXxx()` / `app.MapXxx()` on `IEndpointRouteBuilder` or `WebApplication`

All registered in `Program.cs`:
```csharp
builder.AddScraperSettings();
builder.AddBlazor();
builder.AddMediatorModule();
// ...
app.UseCvFeatures();
app.UseJobs();
```

## Code Patterns & Conventions

### Vertical Slice Handler Pattern (Nested Types)

Business logic uses **nested** Command/Response/Handler inside a feature class:

```csharp
public class DuplicateCv
{
    public record Command(...) : IRequest<ErrorOr<Response>>;
    public record Response(long Id);

    public class Handler(JobsDbContext dbContext, IValidator<CvEntity> validator)
        : IRequestHandler<Command, ErrorOr<Response>>
    {
        public async ValueTask<ErrorOr<Response>> Handle(Command command, CancellationToken ct)
        {
            // ...
            return new Response(id);
        }
    }
}
```

### Scraper Handler Hierarchy

Scrapers follow an inheritance chain:
- `ScrapperBaseHandler` → base with config, logger, DbContext
- `ListScraperBaseHandler<T>` → list scraping with `IAsyncEnumerable<List<JobOffer>> ScrapeJobs()`
- `DetailsScrapperBaseHandler<T>` → detail scraping
- Concrete scrapers: `JjitListScraper`, `IndeedDetailsScraper`, etc.

Each concrete scraper defines a nested `Command` record:
```csharp
public class JjitListScraper
{
    public record Command(SourceConfig Source) : ScrapeCommand(Source);
    // Handler logic...
}
```

### Primary Constructors for DI

All classes use **C# primary constructors** for dependency injection (no field assignments):

```csharp
public sealed partial class ScrapeHandler(
    IMediator mediator,
    UserProvider userProvider,
    JobsDbContext dbContext,
    ILogger<ScrapeHandler> logger)
{ }
```

### Source-Generated Logging

Use `[LoggerMessage]` attribute on `static partial void` methods in `partial` classes:

```csharp
public sealed partial class ScrapeHandler(...)
{
    [LoggerMessage(LogLevel.Information, "Scraping {UserName}")]
    private static partial void LogScrapingInProgress(ILogger logger, string UserName);
}
```

### Entity Configuration

Entities and their EF Core configurations live in the **same file** under `Common/Entities/`:

```csharp
public class JobOffer : IUpdatable
{
    public string OfferUrl { get; set; } = null!;
    // properties...
}

public class JobOfferModelBuilder : IEntityTypeConfiguration<JobOffer>
{
    public void Configure(EntityTypeBuilder<JobOffer> builder) { /* ... */ }
}
```

Applied in `JobsDbContext.OnModelCreating` via `modelBuilder.ApplyConfiguration(new XxxModelBuilder())`.

### Result Types

- Use `ErrorOr<T>` for operations that can fail
- Return `Error.Failure(description: "...")` or `Error.Validation(metadata: ...)` for errors

### Blazor Pages

- Pages use **code-behind** files (`.razor.cs`) with `partial class` and primary constructors
- Inject services via primary constructor: `public partial class ScrapePage(IDbContextFactory<JobsDbContext> dbFactory, ...)`
- Use `IDbContextFactory<JobsDbContext>` to create scoped DbContext instances
- FluentValidation integrated via `Blazored.FluentValidation`

### Minimal API Endpoints

Feature endpoints use `MapGroup` with static handler methods:

```csharp
public static class CvPdfEndpoints
{
    public static IEndpointRouteBuilder MapCvPdfEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cv").RequireAuthorization();
        group.MapGet("/{id:long}/pdf", GetPdf);
        return app;
    }

    private static async Task<IResult> GetPdf(...) { /* ... */ }
}
```

### DbContext & Multi-tenancy

- `JobsDbContext` extends `IdentityDbContext<ApplicationUser>` with ownership filtering
- `CurrentUserName` property drives global query filters via `ApplyOwnershipFilter()`
- Uses **pooled factory** with `IDbContextFactory<JobsDbContext>`, decorated by `UserJobsContextFactory`
- Interceptors: `UpdatableInterceptor` (auto `UpdatedAt`), `OwnerInterceptor`

## Naming & Style Conventions

- **File-scoped namespaces**: `namespace X.Y.Z;`
- **`var`** preferred everywhere
- **No `this.`** qualification
- **`sealed`** on handler classes when possible
- **Private readonly fields**: `_camelCase`; private non-readonly: `camelCase`
- **Constants**: `PascalCase`
- **Braces**: optional for single statements (`csharp_prefer_braces = false`)
- **4 spaces** indentation, trailing commas in multiline lists
- Full `.editorconfig` in project root

## Testing Conventions

- **Framework**: xUnit (`[Fact]`, `[Theory]`)
- **Assertions**: Shouldly (`result.ShouldBe(...)`, `result.IsError.ShouldBeFalse()`)
- **Mocking**: NSubstitute
- **Test structure**: mirrors feature folder structure under `JobScraper.Tests/Features/` and `JobScraper.Tests/Logic/`
- **Naming**: `{ClassName}Tests` with descriptive method names
- Direct handler instantiation (no DI container in tests)

## Build & Run

- **Solution file**: `JobScraper.slnx`
- **Target**: `net10.0` (set in `Directory.Build.props`)
- **Database**: SQLite (connection string in `appsettings.Development.json`)
- **Docker**: `compose.yaml` in root, Linux target OS
- **Scripts**: `scripts/` folder (PowerShell: migrations, publish, docker push)
- **EF Migrations**: `scripts/add-migration.ps1`, `scripts/rollback-migration.ps1`
- DB auto-migrates on startup via `app.Services.PrepareDbAsync()`

## Key Embedded Resources

- `Features/JobOffers/Scrape/Logic/Jjit/jjit-list.js` — JS for Playwright scraping
- `Features/JobOffers/Scrape/Logic/NoFluffJobs/no-fluff-jobs-details.js`, `no-fluff-jobs-list.js`
