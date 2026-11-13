FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY AstronautHealthMonitoring.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish AstronautHealthMonitoring.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --start-period=60s --retries=5 CMD curl --fail http://127.0.0.1:8080/health || exit 1
USER $APP_UID
ENTRYPOINT ["dotnet", "AstronautHealthMonitoring.dll"]
