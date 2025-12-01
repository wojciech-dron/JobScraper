FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

USER $APP_UID
EXPOSE 8080
EXPOSE 8081

ENV CONNECTIONSTRINGS__DEFAULTCONNECTION="data source=/home/app/data/Jobs.db"
ENV SECURITYSETTINGS__PERSISTKEYSDIRECTORY="/home/app/data/keys"
ENV APPSETTINGS__PAGESAVINGDIRECTORY="/home/app/data/jobs"
ENV APPSETTINGS__PREINSTALLEDPLAYWRIGHT=true

# Switch to root to install dependencies
USER root

# Install PowerShell for Playwright scripts
RUN apt-get update -yq \
    && apt-get install -yq wget gnupg2 \
    && wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update -yq \
    && apt-get install -yq powershell \
    && rm -rf /var/lib/apt/lists/*  # Cleanup to reduce image size

# Switch back to the application user
USER $APP_UID

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["JobScraper.Web/JobScraper.Web.csproj", "JobScraper.Web/"]
COPY ["JobScraper/JobScraper.csproj", "JobScraper/"]
COPY ["JobScraper.Common/JobScraper.Common.csproj", "JobScraper.Common/"]
RUN dotnet restore "JobScraper.Web/JobScraper.Web.csproj"
COPY . .
WORKDIR "/src/JobScraper.Web"
RUN dotnet build "JobScraper.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "JobScraper.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /home/app
COPY --from=publish /app/publish .

# Install Playwright chrome
USER root
RUN pwsh -c "./playwright.ps1 install --with-deps firefox"

# Haha that's a tricky one, took 4 hours to figure that playwright with root installs browser on a root/.cache folder
RUN mv /root/.cache /home/app

# Set the permissions for the directories created by root for app user
RUN chmod -R 777 /home/app

USER $APP_UID

# Create data directory and volume for data
RUN mkdir -p /home/app/data
VOLUME /home/app/data

ENTRYPOINT ["dotnet", "JobScraper.Web.dll"]
