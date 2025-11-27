# Adding migration
dotnet ef migrations add --project JobScraper/JobScraper.csproj [Name]

# Updating database
dotnet ef database update --project JobScraper/JobScraper.csproj [Name]

# Remove migration
dotnet ef migrations remove --project JobScraper/JobScraper.csproj [Name]
