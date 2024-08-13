FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /build
COPY ["src/Kobalt/Kobalt.Bot/Kobalt.Bot.csproj", "src/Kobalt/Kobalt.Bot/"]
COPY ["src/InfractionsAPI/Kobalt.Infractions.Shared/Kobalt.Infractions.Shared.csproj", "src/InfractionsAPI/Kobalt.Infractions.Shared/"]
COPY ["src/PhishingAPI/Kobalt.Phishing.Shared/Kobalt.Phishing.Shared.csproj", "src/PhishingAPI/Kobalt.Phishing.Shared/"]
COPY ["src/Kobalt/Kobalt.Bot.Data/Kobalt.Bot.Data.csproj", "src/Kobalt/Kobalt.Bot.Data/"]
COPY ["src/Kobalt/Kobalt.Shared/Kobalt.Shared.csproj", "src/Kobalt/Kobalt.Shared/"]
COPY ["src/Kobalt/Kobalt.Infrastructure/Kobalt.Infrastructure.csproj", "src/Kobalt/Kobalt.Infrastructure/"]
RUN dotnet restore "src/Kobalt/Kobalt.Bot/Kobalt.Bot.csproj"
COPY . .
WORKDIR "/build/src/Kobalt/Kobalt.Bot"
RUN dotnet build "Kobalt.Bot.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Kobalt.Bot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kobalt.Bot.dll"]
