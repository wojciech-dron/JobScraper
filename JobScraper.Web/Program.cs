using FluentValidation;
using JobScraper.Auth;
using JobScraper.Jobs;
using JobScraper.OpenTelemetry;
using JobScraper.Persistence;
using JobScraper.Security;
using JobScraper.Settings;
using JobScraper.Web.Blazor;
using JobScraper.Web.Modules.Mediator;
using JobScraper.Web.Scraping;

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

app.UseHttpsRedirection();
app.UseExceptionHandler("/Error", true);
app.UseBlazor();
app.UseJobs();


app.Run();
