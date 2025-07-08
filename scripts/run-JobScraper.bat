cd JobScraper

:: install playwright
:: pwsh .\playwright.ps1 install

:: run app
start http://localhost:5000
.\JobScraper.Web.exe
