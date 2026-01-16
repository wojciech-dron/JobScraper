using FluentValidation;
using JobScraper.Web.Blazor;
using JobScraper.Web.Features.JobOffers.Scrape;
using JobScraper.Web.Modules.Auth;
using JobScraper.Web.Modules.Jobs;
using JobScraper.Web.Modules.Mediator;
using JobScraper.Web.Modules.OpenTelemetry;
using JobScraper.Web.Modules.Persistence;
using JobScraper.Web.Modules.Security;
using JobScraper.Web.Modules.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.AddScraperSettings();
builder.AddBlazor();
builder.AddMediatorModule();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.AddAuthServices();
builder.AddLogging();
builder.AddPersistence();
builder.AddJobs();
builder.ConfigureSecurity();
builder.AddScrapeServices();


var app = builder.Build();

await app.Services.PrepareDbAsync();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseExceptionHandler("/Error", true);
app.UseBlazor();
app.UseJobs();


app.Run();
