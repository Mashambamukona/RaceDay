# RaceDay

Race event management system.

- **RaceDay.Api** - ASP.NET Core Web API (.NET 8, SQL Server, Azure Blob Storage for banner images)
- **RaceDay.Web** - ASP.NET Core MVC website that calls the API
- **RaceDay.Tests** - xUnit tests for the API

## Demo video

https://youtu.be/YOUR_LINK_HERE

## Run the API

From the `Part2-API` folder:

    cd RaceDay.Api
    dotnet run

The API runs on http://localhost:5080 (Swagger at http://localhost:5080/swagger).

Banner uploads need an Azure Storage connection string, set with user secrets:

    dotnet user-secrets set "AzureStorage:ConnectionString" "<your connection string>"

Without it, everything else still works; only the banner upload is skipped.

## Run the website with Docker

With the API running on your computer:

    docker build -t raceday-web .
    docker run -p 8080:8080 -e ApiBaseUrl=http://host.docker.internal:5080/ raceday-web

Then open http://localhost:8080.

## Run the tests

    dotnet test Part2-API/RaceDay.sln

## CI

GitHub Actions (`.github/workflows/ci.yml`) builds the API, tests and website, runs the tests, and builds the Docker image on every push and pull request.
