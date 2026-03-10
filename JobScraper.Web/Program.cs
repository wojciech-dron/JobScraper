using FluentValidation;
using JobScraper.Web.Blazor;
using JobScraper.Web.Features.Cv;
using JobScraper.Web.Features.JobOffers.Scrape;
using JobScraper.Web.Integration;
using JobScraper.Web.Modules.Auth;
using JobScraper.Web.Modules.Jobs;
using JobScraper.Web.Modules.Logging;
using JobScraper.Web.Modules.Mediator;
using JobScraper.Web.Modules.Persistence;
using JobScraper.Web.Modules.QuestPdf;
using JobScraper.Web.Modules.Security;
using JobScraper.Web.Modules.Services;
using JobScraper.Web.Modules.Settings;
using JobScraper.Web.Modules.TimeProvider;

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
builder.Services.AddUserProvider();
builder.Services.AddTimeProvider();
builder.AddIntegrationServices();
builder.AddQuestPdf();


var app = builder.Build();

await app.Services.PrepareDbAsync();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseAppLogging();
app.UseExceptionHandler("/Error", true);
app.UseUserIdentityMiddleware();
app.UseBlazor();
app.UseCvFeatures();
app.UseJobs();
app.UseIntegrationServices();


app.Run();
