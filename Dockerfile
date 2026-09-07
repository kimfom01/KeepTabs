FROM mcr.microsoft.com/dotnet/aspnet:10.0.0-alpine3.22 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# Builds the React SPA into KeepTabs/wwwroot so the API serves UI and API as one app.
FROM node:24-alpine AS frontend
RUN npm install -g pnpm@11
WORKDIR /src/front-src
COPY ["front-src/package.json", "front-src/pnpm-lock.yaml", "./"]
RUN pnpm install --frozen-lockfile
COPY front-src/ ./
RUN pnpm build

FROM mcr.microsoft.com/dotnet/sdk:10.0.100-alpine3.22 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["KeepTabs/KeepTabs.csproj", "KeepTabs/"]
COPY ["KeepTabs.Application/KeepTabs.Application.csproj", "KeepTabs.Application/"]
COPY ["KeepTabs.Domain/KeepTabs.Domain.csproj", "KeepTabs.Domain/"]
COPY ["KeepTabs.Infrastructure/KeepTabs.Infrastructure.csproj", "KeepTabs.Infrastructure/"]
COPY ["ServiceDefaults/ServiceDefaults.csproj", "ServiceDefaults/"]
RUN dotnet restore "KeepTabs/KeepTabs.csproj"
COPY . .
COPY --from=frontend /src/KeepTabs/wwwroot ./KeepTabs/wwwroot
WORKDIR "/src/KeepTabs"
RUN dotnet build "KeepTabs.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/build /p:BuildFrontend=false

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "KeepTabs.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/publish /p:UseAppHost=false /p:BuildFrontend=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KeepTabs.dll"]
