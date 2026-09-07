FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TroyOS.Api/TroyOS.Api.csproj TroyOS.Api/
RUN dotnet restore TroyOS.Api/TroyOS.Api.csproj

COPY TroyOS.Api/ TroyOS.Api/
RUN dotnet publish TroyOS.Api/TroyOS.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    --property:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TroyOS.Api.dll"]
