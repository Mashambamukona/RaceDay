# Build the RaceDay website (ASP.NET Core MVC, .NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Part3-MVC/ Part3-MVC/
RUN dotnet restore Part3-MVC/RaceDay.Web/RaceDay.Web.csproj
RUN dotnet publish Part3-MVC/RaceDay.Web/RaceDay.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
