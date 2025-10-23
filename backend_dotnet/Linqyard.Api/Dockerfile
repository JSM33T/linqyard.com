# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
ARG APP_UID=64198
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

USER root
RUN mkdir -p /app/logs/dotnet /app/cache && chown -R ${APP_UID}:${APP_UID} /app/logs /app/cache
USER ${APP_UID}

# Build
ARG DOTNET_SDK_VERSION=9.0.306   # <— update
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy global.json first so restore uses the right SDK and caches better
COPY global.json ./

# Restore using project file(s)
COPY Linqyard.Api/Linqyard.Api.csproj Linqyard.Api/
RUN dotnet restore Linqyard.Api/Linqyard.Api.csproj

# Copy the rest
COPY . .
WORKDIR /src/Linqyard.Api

RUN dotnet build Linqyard.Api.csproj -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/Linqyard.Api
RUN dotnet publish Linqyard.Api.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final
FROM base AS final
ARG APP_UID=64198
USER root
RUN mkdir -p /app/logs/dotnet && chown -R ${APP_UID}:${APP_UID} /app/logs
USER ${APP_UID}

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Linqyard.Api.dll"]
