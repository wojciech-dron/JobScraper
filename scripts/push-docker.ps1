param (
    [Parameter(Mandatory = $true)]
    [string]$tagName
)

# Set root directory
cd ..

# 1. Authenticate with Docker Hub
Write-Host "Logging into Docker..." -ForegroundColor Cyan
docker login

# 2. Build the image
Write-Host "Building image: jobscraper.web..." -ForegroundColor Cyan
docker build -t jobscraper.web -f Jobscraper.Web/Dockerfile .

# Check if build succeeded
if ($LASTEXITCODE -ne 0)
{
    Write-Error "Docker build failed. Exiting."; exit $LASTEXITCODE
}

# 3. Tag and Push 'latest'
Write-Host "Pushing tag: latest..." -ForegroundColor Cyan
docker tag jobscraper.web combi71/jobscraper.web:latest
docker push combi71/jobscraper.web:latest

# 4. Tag and Push the specific version provided in argument
Write-Host "Pushing tag: $tagName..." -ForegroundColor Cyan
docker tag jobscraper.web "combi71/jobscraper.web:$tagName"
docker push "combi71/jobscraper.web:$tagName"

Write-Host "Deployment complete!" -ForegroundColor Green
