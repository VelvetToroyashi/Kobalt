FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/InfractionsAPI/Kobalt.Infractions.API/Kobalt.Infractions.API.csproj", "src/InfractionsAPI/Kobalt.Infractions.API/"]
COPY ["src/Kobalt/Kobalt.Shared/Kobalt.Shared.csproj", "src/Kobalt/Kobalt.Shared/"]
COPY ["src/InfractionsAPI/Kobalt.Infractions.Data/Kobalt.Infractions.Data.csproj", "src/InfractionsAPI/Kobalt.Infractions.Data/"]
COPY ["src/InfractionsAPI/Kobalt.Infractions.Shared/Kobalt.Infractions.Shared.csproj", "src/InfractionsAPI/Kobalt.Infractions.Shared/"]
RUN dotnet restore "src/InfractionsAPI/Kobalt.Infractions.API/Kobalt.Infractions.API.csproj"
COPY . .
WORKDIR "/src/src/InfractionsAPI/Kobalt.Infractions.API"
RUN dotnet build "Kobalt.Infractions.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Kobalt.Infractions.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kobalt.Infractions.API.dll"]
