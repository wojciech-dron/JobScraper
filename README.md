# Job Scraper with C# and Playwright

https://github.com/wojciech-dron/JobScraper

This project uses C# and Microsoft Playwright to scrape job data from Indeed and JustJoin.It (for now).

## Launch app

Run 

```bash
run-JobScraper.bat
```

You can modify search parameters modifying filters in searching sites
and re-paste SearchUrl in scraperSettings.json 



## Development setup

This project requires .NET and the Microsoft Playwright CLI. If you don't have these installed, follow the instructions below.

1. Install .NET from the [.NET download page](https://dotnet.microsoft.com/download).

2. Install the Microsoft Playwright CLI by running the following command in your terminal:

   ```bash
   dotnet tool install --global Microsoft.Playwright.CLI
   ```

3. After installing the Playwright CLI, run the following command to install the necessary browser binaries:

   ```bash
   playwright install
   ```

## Running the Project

1. Clone the repository and navigate to the project directory in your terminal.

2. Run the following command to restore the necessary .NET packages:

   ```bash
   dotnet restore
   ```

3. (Optional) Open the project in your favorite IDE.


<span style="color: green">**Recommended:**</span> Be nice to people's servers by not lowering the `secondsToWait` variable too low. <font size="1">(keep yourself from being banned from the site)</font>
