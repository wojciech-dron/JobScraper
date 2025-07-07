# Job Scraper with C# and Playwright

JobScraper - an application for managing job offers and applications for
individual offers (like a better Excel)
App is currently targeting polish users.
It's written in C# using Playwright for web scraping.

Link for app: https://drive.google.com/drive/folders/1r7SKJV40hwNzg7KKmsiBF6JOrXXAzTmr

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

Source code link: https://github.com/wojciech-dron/JobScraper

Let me know what you think of the app, your experience and what could be improved - 
I appreciate any feedback ;)


## Development setup

This project requires .NET and the Microsoft Playwright CLI. If you don't have these installed, follow the instructions below.

1. Install .NET 9 from the [.NET download page](https://dotnet.microsoft.com/download).

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
