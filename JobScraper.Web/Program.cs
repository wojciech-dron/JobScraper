using System.Globalization;
using FluentValidation;
using JobScraper;
using JobScraper.Common;
using JobScraper.Jobs;
using JobScraper.Modules.Auth;
using JobScraper.Modules.OpenTelemetry;
using JobScraper.Modules.Wolverine;
using JobScraper.Persistence;
using JobScraper.Web.Blazor;
using JobScraper.Web.Validators;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using NReco.Logging.File;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddScrapperSettings(builder.Environment);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddQuickGridEntityFrameworkAdapter();
builder.Services.AddBlazorBootstrap();
builder.Services.AddValidatorsFromAssemblyContaining<JobOfferValidator>();
builder.AddWolverineServices();
builder.AddAuthServices();

builder.Logging
    .AddFile(builder.Configuration.GetSection("Logging:File"))
    .AddOtelLogging(builder.Configuration, "JobScraper.Web");

builder.Services
    .AddSqlitePersistance()
    .AddScrapperServices(builder.Configuration);


builder.Services.AddJobs(builder.Configuration);

builder.WebHost.UseStaticWebAssets();

builder.Services.AddDataProtection()
    .SetApplicationName("JobScraper.Web")
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["SecuritySettings:PersistKeysDirectory"]!));

var app = builder.Build();

await app.Services.PrepareDbAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// pl
var supportedCultures = new[]
{
    new CultureInfo("pl-PL"),
};
app.UseRequestLocalization(new RequestLocalizationOptions
{
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
});

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseStaticFiles();

app.MapStaticAssets();

app.UseJobs();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
