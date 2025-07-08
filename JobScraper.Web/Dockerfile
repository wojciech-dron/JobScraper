FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
ENV DOTNET_ENVIRONMENT=Docker
ENV CONNECTIONSTRINGS__DEFAULTCONNECTION="data source=/app/data/Jobs.db"
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["JobScraper.Web/JobScraper.Web.csproj", "JobScraper.Web/"]
COPY ["JobScraper/JobScraper.csproj", "JobScraper/"]
COPY ["JobScraper.Common/JobScraper.Common.csproj", "JobScraper.Common/"]
RUN dotnet restore "JobScraper.Web/JobScraper.Web.csproj"
COPY . .
WORKDIR "/src/JobScraper.Web"
RUN dotnet build "JobScraper.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "JobScraper.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/data
VOLUME /app/data
ENTRYPOINT ["dotnet", "JobScraper.Web.dll"]
