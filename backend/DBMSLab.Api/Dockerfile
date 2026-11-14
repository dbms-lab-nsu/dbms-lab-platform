FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish

FROM build AS migrations
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.13
ENTRYPOINT ["/tools/dotnet-ef", "database", "update", "--no-build", "--configuration", "Release", "--project", "/src/DBMSLab.Api.csproj"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "DBMSLab.Api.dll"]
