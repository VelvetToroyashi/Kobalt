FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["src/PhishingAPI/Kobalt.Phishing.API/Kobalt.Phishing.API.csproj", "src/PhishingAPI/Kobalt.Phishing.API/"]
COPY ["src/Kobalt/Kobalt.Shared/Kobalt.Shared.csproj", "src/Kobalt/Kobalt.Shared/"]
COPY ["src/PhishingAPI/Kobalt.Phishing.Data/Kobalt.Phishing.Data.csproj", "src/PhishingAPI/Kobalt.Phishing.Data/"]
COPY ["src/PhishingAPI/Kobalt.Phishing.Shared/Kobalt.Phishing.Shared.csproj", "src/PhishingAPI/Kobalt.Phishing.Shared/"]
RUN dotnet restore "src/PhishingAPI/Kobalt.Phishing.API/Kobalt.Phishing.API.csproj"
COPY . .
WORKDIR "/src/src/PhishingAPI/Kobalt.Phishing.API"
RUN dotnet build "Kobalt.Phishing.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kobalt.Phishing.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kobalt.Phishing.API.dll"]
