FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY CleaningService.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c debug -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "CleaningService.dll"]

ENV ASPNETCORE_ENVIRONMENT=Development