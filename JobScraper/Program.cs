using Cocona;
using JobScraper;
using JobScraper.Common;
using JobScraper.Logic;
using JobScraper.Logic.Indeed;
using JobScraper.Logic.Jjit;
using JobScraper.Logic.NoFluffJobs;
using JobScraper.Logic.Olx;
using JobScraper.Logic.PracujPl;
using JobScraper.Logic.RocketJobs;
using JobScraper.Persistence;
using MediatR;

var builder = CoconaApp.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddScrapperSettings(builder.Environment);

builder.Logging.AddOtelLogging(builder.Configuration, "JobScraper");

builder.Services
    .AddSqlitePersistance()
    .AddScrapperServices(builder.Configuration);

var app = builder.Build();
await app.Services.PrepareDbAsync();

app.AddCommands<Commands>();

// if command does not execute, check if handler dependencies are registered.
await app.RunAsync();

Console.WriteLine("Scrapper finished");


public class Commands(IMediator mediator)
{
    [PrimaryCommand]
    public async Task ScrapeAll() => await mediator.Send(new ScrapePipeline.Request());

    public async Task IndeedList() => await mediator.Send(new IndeedListScraper.Command());
    public async Task IndeedDetails() => await mediator.Send(new IndeedDetailsScraper.Command());

    public async Task JjitList() => await mediator.Send(new JjitListScraper.Command());
    public async Task JjitDetails() => await mediator.Send(new JjitDetailsScraper.Command());

    public async Task NoFluffJobsList() => await mediator.Send(new NoFluffJobsListScraper.Command());
    public async Task NoFluffJobsDetails() => await mediator.Send(new NoFluffJobsDetailsScraper.Command());

    public async Task PracujPlList() => await mediator.Send(new PracujPlListScraper.Command());

    public async Task RocketJobsDetails() => await mediator.Send(new RocketJobsDetailsScraper.Command());
    public async Task RocketJobsList() => await mediator.Send(new RocketJobsListScraper.Command());

    public async Task OlxJobsList() => await mediator.Send(new OlxListScraper.Command());
}