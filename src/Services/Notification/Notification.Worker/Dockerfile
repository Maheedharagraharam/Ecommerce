FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/Notification/Notification.Worker/Notification.Worker.csproj", "src/Services/Notification/Notification.Worker/"]
COPY ["src/BuildingBlocks/BuildingBlocks.Common/BuildingBlocks.Common.csproj", "src/BuildingBlocks/BuildingBlocks.Common/"]
RUN dotnet restore "src/Services/Notification/Notification.Worker/Notification.Worker.csproj"
COPY . .
WORKDIR "/src/src/Services/Notification/Notification.Worker"
RUN dotnet build "Notification.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Notification.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notification.Worker.dll"]
