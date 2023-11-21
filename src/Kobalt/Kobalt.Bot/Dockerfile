FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["src/Kobalt/Kobalt.Bot/Kobalt.Bot.csproj", "src/Kobalt/Kobalt.Bot/"]
COPY ["src/InfractionsAPI/Kobalt.Infractions.Shared/Kobalt.Infractions.Shared.csproj", "src/InfractionsAPI/Kobalt.Infractions.Shared/"]
COPY ["src/PhishingAPI/Kobalt.Phishing.Shared/Kobalt.Phishing.Shared.csproj", "src/PhishingAPI/Kobalt.Phishing.Shared/"]
COPY ["src/Kobalt/Kobalt.Bot.Data/Kobalt.Bot.Data.csproj", "src/Kobalt/Kobalt.Bot.Data/"]
COPY ["src/Kobalt/Kobalt.Shared/Kobalt.Shared.csproj", "src/Kobalt/Kobalt.Shared/"]
COPY ["src/Kobalt/Kobalt.Infrastructure/Kobalt.Infrastructure.csproj", "src/Kobalt/Kobalt.Infrastructure/"]
RUN dotnet restore "src/Kobalt/Kobalt.Bot/Kobalt.Bot.csproj"
COPY . .
WORKDIR "/src/src/Kobalt/Kobalt.Bot"
RUN dotnet build "Kobalt.Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kobalt.Bot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kobalt.Bot.dll"]
