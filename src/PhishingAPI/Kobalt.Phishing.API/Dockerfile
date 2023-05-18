FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/PhishingAPI/Kobalt.Phishing.API/Kobalt.Phishing.API.csproj", "src/PhishingAPI/Kobalt.Phishing.API/"]
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
