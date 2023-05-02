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

RUN for file in ./Plugins/*; do \
    if [ -d "$file" ]; then \
        echo "Building plugin $file"; \
        dotnet publish "$file" -c Release -o /app/publish/plugin_cache/$(basename "$file") /p:UseAppHost=false ; \
        cp -r /app/publish/plugin_cache/$(basename "$file")/ /app/publish/Plugins/ ; \
    fi; \
done

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kobalt.Core.dll"]
