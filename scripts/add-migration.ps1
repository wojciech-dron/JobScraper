param (
    [Parameter(Mandatory = $false, Position = 0)]
    [string]$MigrationName
)

if (-not $MigrationName)
{
    $MigrationName = Read-Host "Enter migration name"
}

if (-not $MigrationName)
{
    Write-Error "Migration name is required."
    exit 1
}

$ProjectPath = "../JobScraper.Web/JobScraper.Web.csproj"

Write-Host "Adding migration '$MigrationName'..." -ForegroundColor Cyan

dotnet ef migrations add $MigrationName --project $ProjectPath --startup-project $ProjectPath

if ($LASTEXITCODE -eq 0)
{
    Write-Host "Migration '$MigrationName' added successfully." -ForegroundColor Green
}
else
{
    Write-Error "Failed to add migration."
}
