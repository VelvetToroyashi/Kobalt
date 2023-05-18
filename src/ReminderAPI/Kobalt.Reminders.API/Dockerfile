FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Kobalt.Reminders.API/Kobalt.Reminders.API.csproj", "Kobalt.Reminders.API/"]
RUN dotnet restore "Kobalt.Reminders.API/Kobalt.Reminders.API.csproj"
COPY . .
WORKDIR "/src/Kobalt.Reminders.API"
RUN dotnet build "Kobalt.Reminders.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kobalt.Reminders.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kobalt.Reminders.API.dll"]
