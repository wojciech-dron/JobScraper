param (
    [Parameter(Mandatory = $false, Position = 0)]
    [int]$Count
)

if (-not $Count)
{
    $Count = Read-Host "Enter number of migrations to rollback"
}

if (-not $Count -or $Count -le 0)
{
    Write-Error "A positive number of migrations is required."
    exit 1
}

$ProjectPath = "../JobScraper.Web/JobScraper.Web.csproj"

# Get the list of applied migrations
$migrations = dotnet ef migrations list --project $ProjectPath --startup-project $ProjectPath --no-connect
if ($LASTEXITCODE -ne 0)
{
    Write-Error "Failed to list migrations."
    exit 1
}

# Filter out info/header lines, keep only migration names
$migrationNames = $migrations | Where-Object { $_ -and $_ -notmatch '^\s*$' -and $_ -notmatch '^Build' -and $_ -notmatch '^Using' -and $_ -notmatch '^Done' } | ForEach-Object { $_.Trim() }

if ($migrationNames.Count -le $Count)
{
    $targetMigration = "0"
    Write-Host "Rolling back all $($migrationNames.Count) migrations..." -ForegroundColor Cyan
}
else
{
    $targetIndex = $migrationNames.Count - $Count - 1
    $targetMigration = $migrationNames[$targetIndex]
    Write-Host "Rolling back $Count migration(s), targeting '$targetMigration'..." -ForegroundColor Cyan
}

dotnet ef database update $targetMigration --project $ProjectPath --startup-project $ProjectPath

if ($LASTEXITCODE -eq 0)
{
    Write-Host "Successfully rolled back $Count migration(s)." -ForegroundColor Green
}
else
{
    Write-Error "Failed to rollback migrations."
}
