FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["./InfractionsAPI/Kobalt.Infractions.API/Kobalt.Infractions.API.csproj", "src/InfractionsAPI/Kobalt.Infractions.API/"]
COPY ["../../Kobalt/Kobalt.Shared/Kobalt.Shared.csproj", "src/Kobalt/Kobalt.Shared/"]
COPY ["src/InfractionsAPI/Kobalt.Infractions.Data/Kobalt.Infractions.Data.csproj", "src/InfractionsAPI/Kobalt.Infractions.Data/"]
COPY ["src/InfractionsAPI/Kobalt.Infractions.Shared/Kobalt.Infractions.Shared.csproj", "src/InfractionsAPI/Kobalt.Infractions.Shared/"]
RUN dotnet restore "src/InfractionsAPI/Kobalt.Infractions.API/Kobalt.Infractions.API.csproj"
COPY . .
WORKDIR "/src/src/InfractionsAPI/Kobalt.Infractions.API"
RUN dotnet build "Kobalt.Infractions.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kobalt.Infractions.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kobalt.Infractions.API.dll"]
