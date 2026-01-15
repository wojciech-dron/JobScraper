using FluentValidation;
using JobScraper.Auth;
using JobScraper.Jobs;
using JobScraper.Mediator;
using JobScraper.OpenTelemetry;
using JobScraper.Persistence;
using JobScraper.Security;
using JobScraper.Settings;
using JobScraper.Web.Blazor;
using JobScraper.Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.AddScraperSettings();
builder.AddBlazor();
builder.AddWolverineServices();
builder.AddMediator();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.AddAuthServices();
builder.AddLogging();
builder.AddPersistence();
builder.AddJobs();
builder.ConfigureSecurity();


var app = builder.Build();

await app.Services.PrepareDbAsync();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseHttpsRedirection();
app.UseExceptionHandler("/Error", true);
app.UseBlazor();
app.UseJobs();


app.Run();
