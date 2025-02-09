using Cocona;
using JobScraper;
using JobScraper.Common;
using JobScraper.Logic;
using JobScraper.Logic.Indeed;
using JobScraper.Logic.Jjit;
using JobScraper.Logic.NoFluffJobs;
using JobScraper.Persistence;

var builder = CoconaApp.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddScrapperSettings(builder.Environment);

builder.Logging.AddOtelLogging(builder.Configuration, "JobScraper");

builder.Services
    .AddSqlitePersistance()
    .AddScrapperServices(builder.Configuration);

var app = builder.Build();
await app.Services.PrepareDbAsync();

app.AddCommands<IndeedList.Handler>();
app.AddCommands<IndeedDetails.Handler>();
app.AddCommands<JjitList.Handler>();
app.AddCommands<JjitDetails.Handler>();
app.AddCommands<NoFluffJobsList.Handler>();
app.AddCommands<NoFluffJobsDetails.Handler>();

app.AddCommands<ScrapePipeline.Handler>();

// if command does not execute, check if handler dependencies are registered.
await app.RunAsync();

Console.WriteLine("Scrapper finished");