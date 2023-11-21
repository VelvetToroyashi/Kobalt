FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app



FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["src/ReminderAPI/Kobalt.Reminders.API/Kobalt.Reminders.API.csproj", "src/ReminderAPI/Kobalt.Reminders.API/"]
COPY ["src/ReminderAPI/Kobalt.Reminders.Data/Kobalt.Reminders.Data.csproj", "src/ReminderAPI/Kobalt.Reminders.Data/"]
COPY ["src/Kobalt/Kobalt.Shared/Kobalt.Shared.csproj", "src/Kobalt/Kobalt.Shared/"]
RUN dotnet restore "src/ReminderAPI/Kobalt.Reminders.API/Kobalt.Reminders.API.csproj"
COPY . .
WORKDIR "/src/src/ReminderAPI/Kobalt.Reminders.API"
RUN dotnet build "Kobalt.Reminders.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kobalt.Reminders.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kobalt.Reminders.API.dll"]
