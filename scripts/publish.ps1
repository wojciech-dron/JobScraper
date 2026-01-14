param (
    [Parameter(Mandatory = $true)]
    [string]$tagName,

    [Parameter(Mandatory = $false)]
    [string]$publishDirectory = "D:\Temp\JobScraper",

    [Parameter(Mandatory = $false)]
    [string]$runtime = "win-x64"
)

# Set root directory
cd ..

# 1. Setup Dynamic Output Path
# This joins the base directory with the tag name (e.g., D:\Temp\JobScraper\0.4)
$finalOutputPath = Join-Path -Path $publishDirectory -ChildPath $tagName

# 2. Dotnet Publish (Self-Contained)
Write-Host "Publishing self-contained deployment to $finalOutputPath..." -ForegroundColor Cyan
if (!(Test-Path $finalOutputPath))
{
    New-Item -ItemType Directory -Force -Path $finalOutputPath | Out-Null
}

dotnet publish Jobscraper.Web/Jobscraper.Web.csproj `
    --configuration Release `
    --runtime $runtime `
    --self-contained true `
    --output $finalOutputPath

if ($LASTEXITCODE -ne 0)
{
    Write-Error "Dotnet publish failed."; exit $LASTEXITCODE
}

# 3. Git Tagging
Write-Host "Creating Git tag: $tagName..." -ForegroundColor Cyan
# Checking if tag exists to avoid errors
$tagExists = git tag -l $tagName
if ($tagExists)
{
    Write-Warning "Tag $tagName already exists in Git. Skipping creation."
}
else
{
    git tag $tagName
}
