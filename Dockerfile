﻿FROM mcr.microsoft.com/dotnet/aspnet:9.0.0-alpine3.20 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0.101-alpine3.20 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["KeepTabs/KeepTabs.csproj", "KeepTabs/"]
RUN dotnet restore "KeepTabs/KeepTabs.csproj"
COPY . .
WORKDIR "/src/KeepTabs"
RUN dotnet build "KeepTabs.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "KeepTabs.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KeepTabs.dll"]
