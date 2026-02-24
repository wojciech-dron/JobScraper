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
- to update application, extract new version and copy Data folder from the
  previous version and replace it with the new one
- to delete all data, just delete the Data folder
- updating the app is safe, it won't delete data from the previous version,
  however, moving data from a newer version to an older one won't work

Let me know what you think of the app, your experience and what could be
improved -
I appreciate any feedback ;)

## Demo

Site is available at https://jobscraper.wojciechdron.net/

Demo login:
test@email.com
JobScraper1@#

[Configuration and scraping](https://drive.google.com/file/d/1D-sGQ3w9u8nb9_HW_olHNEMGFR9nUWQm/view)

[Managing offers and applications (v0.0.1)](https://drive.google.com/file/d/1nu9P4w3vn8zJl3TTss1zsLBxoxjgEGqP/view)

## Quick start with docker

To run the app using Docker, you can use the following command:

```bash
docker run -d --name job-scraper -p 12986:8080 -v jobscraper_data:/home/app/data combi71/jobscraper.web:latest
```

Sorry for its size, probably preinstalled playwright with browser.

## Quick deployment with Docker Compose

To run the application with its full stack (including a remote browser for
scraping and optional AI features), you can use Docker Compose.

1. **Copy compose.yaml to a new directory for deployment config**:

2. **Prepare configuration**:
   Copy `.env.example` to `.env` and fill in the required values:
    - `TICKER_PASSWORD`: Password for the TickerQ dashboard.
    - `OPENROUTER_APIKEY`: Your API key for AI features (OpenRouter).

   You can also use other OpenAi compatible providers.

3. **Start the services**:
   ```bash
   docker compose up -d
   ```

This will start:

- **job-scraper**: The main application available at http://localhost:12986.
- **chrome**: A browserless Chrome instance used for scraping, avoiding the need
  for a local browser installation.

**Data Persistence**:
By default, the application data is stored in the `~/JobScraper/Data` folder on
the host (mapped to `/home/app/data` in the container), as configured in
`compose.yaml`. You can adjust the host path in `compose.yaml` if needed.

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

## Troubleshooting

### Docker file access/permissions issues

If you encounter issues with file access or permissions when running the
application with Docker, particularly related to the volume mount for data
persistence, try setting appropriate permissions on the host directory.

For example, if using the data volume, ensure the directory
has proper permissions:

```bash
chmod -R 755 ~/JobScraper/Data/
```

This ensures that the Docker container can read and write to the data directory
properly.

