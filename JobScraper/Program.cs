using Cocona;
using JobScraper;
using JobScraper.Logic;
using JobScraper.Persistence;
using JobScraper.Utils;

var builder = CoconaApp.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

builder.Logging.AddOtelLogging(builder.Configuration);

builder.Services
    .AddSqlitePersistance()
    .AddScrapperServices(builder.Configuration);

var app = builder.Build();
await app.Services.PrepareDbAsync();

// app.AddCommands<IndeedList.Handler>();
// app.AddCommands<IndeedDetails.Handler>();
app.AddCommands<JjitList.Handler>();
app.AddCommands<JjitDetails.Handler>();

app.AddCommands<ScrapePipeline.Handler>();

await app.RunAsync();

Console.WriteLine("Scrapper finished");