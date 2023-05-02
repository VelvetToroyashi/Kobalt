FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src/Kobalt

COPY "./Kobalt.Core/Kobalt.Core.csproj" "/src/Kobalt/Kobalt.Core/Kobalt.Core.csproj"
COPY "./Kobalt.Infrastructure/Kobalt.Infrastructure.csproj" "/src/Kobalt/Kobalt.Infrastructure/Kobalt.Infrastructure.csproj"
COPY "./Kobalt.Shared/Kobalt.Shared.csproj" "/src/Kobalt/Kobalt.Shared/Kobalt.Shared.csproj"
RUN dotnet restore "./Kobalt.Core/Kobalt.Core.csproj"

FROM build AS publish
COPY ./ ./
RUN dotnet publish "./Kobalt.Core/Kobalt.Core.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kobalt.Core.dll"]
