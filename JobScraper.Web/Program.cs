using System.Globalization;
using FluentValidation;
using FluentValidation.AspNetCore;
using JobScraper;
using JobScraper.Common;
using JobScraper.Models;
using JobScraper.Persistence;
using JobScraper.Web.Components;
using JobScraper.Web.Validators;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddScrapperSettings(builder.Environment);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddQuickGridEntityFrameworkAdapter();
builder.Services.AddBlazorBootstrap();
builder.Services.AddValidatorsFromAssemblyContaining<JobOfferValidator>();

builder.Logging.AddOtelLogging(builder.Configuration, "JobScraper.Web");

builder.Services
    .AddSqlitePersistance()
    .AddScrapperServices(builder.Configuration);

builder.WebHost.UseStaticWebAssets();

builder.Services.AddDataProtection()
    .SetApplicationName("JobScraper.Web")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "Keys")));

var app = builder.Build();

await app.Services.PrepareDbAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// pl
var supportedCultures = new []{ new CultureInfo("pl-PL") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseStaticFiles();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
