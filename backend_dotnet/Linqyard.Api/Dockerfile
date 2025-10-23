# syntax=docker/dockerfile:1.6

# Make SDK version available to any FROM below
ARG DOTNET_SDK_VERSION=9.0.306   # set once; or 9.0.305 if you keep global.json pinned

# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
ARG APP_UID=64198
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

USER root
RUN mkdir -p /app/logs/dotnet /app/cache \
    && chown -R ${APP_UID}:${APP_UID} /app/logs /app/cache
USER ${APP_UID}

# Build
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy global.json first so the resolver picks the right SDK (and cache works)
COPY global.json ./

# Restore
COPY Linqyard.Api/Linqyard.Api.csproj Linqyard.Api/
RUN dotnet restore Linqyard.Api/Linqyard.Api.csproj

# Copy the rest and build
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
