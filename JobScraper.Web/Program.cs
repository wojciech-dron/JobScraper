using FluentValidation;
using FluentValidation.AspNetCore;
using JobScraper;
using JobScraper.Common;
using JobScraper.Models;
using JobScraper.Persistence;
using JobScraper.Web.Components;
using JobScraper.Web.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddScrapperSettings(builder.Environment);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddQuickGridEntityFrameworkAdapter();
builder.Services.AddBlazorBootstrap();
builder.Services.AddValidatorsFromAssemblyContaining<CreateJobOfferValidator>();


builder.Logging.AddOtelLogging(builder.Configuration, "JobScraper.Web");

builder.Services
    .AddSqlitePersistance()
    .AddScrapperServices(builder.Configuration);

var app = builder.Build();

await app.Services.PrepareDbAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
