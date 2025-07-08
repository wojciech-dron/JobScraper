# Job Scraper with C# and Playwright

JobScraper - an application for managing job offers and applications for
individual offers (like a better Excel)
App is currently targeting polish users.
It's written in C# using Playwright for web scraping.

## Main features:
- portability, you can run it on a computer without installing any
  additional programs and from any folder using a .bat file
- fetching job offers from given links (pracuj.pl, rocketjobs.pl, olx.pl, and
  programmer-focused sites: Indeed, Justoin.It, NoFluffJobs)
- job links with filters
- a page with job offers
- manual adding/editing of job offers
- detailed view and entering information about applications to a specific job
- info about applying to a specific company (useful if there are multiple offers
  from the same company)
- a page with user applications (filtering, sorting, and rejecting)
- configuration of sources

## Notes:
- there may be high resource usage because a browser is launched to fetch job
  offers (it can be run in hidden mode - "ShowBrowserWhenScraping": false in
  scraperSettings.json)
- saved job offers and configuration are stored in the Data folder
- to update application, extract new version and copy Data folder from the previous version and replace it with the new one
- to delete all data, just delete the Data folder
- updating the app is safe, it won't delete data from the previous version,
  however, moving data from a newer version to an older one won't work

Let me know what you think of the app, your experience and what could be improved - 
I appreciate any feedback ;)


## Demo

[Configuration and scraping](https://drive.google.com/file/d/1D-sGQ3w9u8nb9_HW_olHNEMGFR9nUWQm/view)

[Managing offers and applications (v0.0.1)](https://drive.google.com/file/d/1nu9P4w3vn8zJl3TTss1zsLBxoxjgEGqP/view)

## Quick start with docker
To run the app using Docker, you can use the following command:
```bash
docker run -d --name jobscraper.web -p 12986:8080 -v jobscraper_data:/home/app/data combi71/jobscraper.web:latest
```

## Local development

### Prerequisites

This project requires .NET and the Microsoft Playwright CLI. If you don't have
these installed, follow the instructions below.

1. Install .NET 9 from
   the [.NET download page](https://dotnet.microsoft.com/download).

2. Install the Microsoft Playwright CLI by running the following command in your
   terminal:

```bash
dotnet tool install --global Microsoft.Playwright.CLI
```

### Running the application
Clone the repository and navigate to the project directory in your terminal.
```bash
git clone https://github.com/wojciech-dron/JobScraper.git
cd JobScraper
```

Run the following command to run project
```bash
cd JobScraper.Web
dotnet run
```
   
## Docker

You can also build and run the project using Docker.

Clone the repository.
```bash
git clone https://github.com/wojciech-dron/JobScraper.git
cd JobScraper
```

Navigate to the project directory in your terminal and run docker compose.
```bash
docker-compose up -d
```

OR

You can also build and run the project using Docker.

```bash
docker build -t jobscraper.web -f Jobscraper.Web/Dockerfile .
```

Then run the following command to start the container.

```bash
docker run -d --name jobscraper.web -p 12986:8080 -v jobscraper_data:/home/app/data jobscraper.web
```





<span style="color: green">**Recommended:**</span> Be nice to people's servers by not lowering the `secondsToWait` variable too low. <font size="1">(keep yourself from being banned from the site)</font>
