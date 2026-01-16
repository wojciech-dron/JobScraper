# Adding migration
dotnet ef migrations add --project JobScraper.Web/JobScraper.Web.csproj [Name]

# Updating database
dotnet ef database update --project JobScraper.Web/JobScraper.Web.csproj [Name]

# Remove migration
dotnet ef migrations remove --project JobScraper.Web/JobScraper.Web.csproj [Name]
