FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Boron/Boron.csproj", "Boron/"]
RUN dotnet restore "Boron/Boron.csproj"
COPY . .
WORKDIR "/src/Boron"
RUN dotnet build "Boron.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Boron.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Boron.dll"]
