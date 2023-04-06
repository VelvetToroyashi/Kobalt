FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Kobalt.ReminderService.API/Kobalt.ReminderService.API.csproj", "Kobalt.ReminderService.API/"]
RUN dotnet restore "Kobalt.ReminderService.API/Kobalt.ReminderService.API.csproj"
COPY . .
WORKDIR "/src/Kobalt.ReminderService.API"
RUN dotnet build "Kobalt.ReminderService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Kobalt.ReminderService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kobalt.ReminderService.API.dll"]
