# # See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# # This stage is used when running from VS in fast mode (Default for Debug configuration)
# FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# USER $APP_UID
# WORKDIR /app
# EXPOSE 8080
# EXPOSE 8081


# # This stage is used to build the service project
# FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# ARG BUILD_CONFIGURATION=Release
# WORKDIR /src
# COPY ["Linqyard.Api/Linqyard.Api.csproj", "Linqyard.Api/"]
# RUN dotnet restore "./Linqyard.Api/Linqyard.Api.csproj"
# COPY . .
# WORKDIR "/src/Linqyard.Api"
# RUN dotnet build "./Linqyard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# # This stage is used to publish the service project to be copied to the final stage
# FROM build AS publish
# ARG BUILD_CONFIGURATION=Release
# RUN dotnet publish "./Linqyard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# # This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
# FROM base AS final
# WORKDIR /app
# COPY --from=publish /app/publish .
# ENTRYPOINT ["dotnet", "Linqyard.Api.dll"]

# See https://aka.ms/customizecontainer ...

# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# App user id (default used by MS images)
ARG APP_UID=64198
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Ensure logs dir exists and is owned by the app user
USER root
RUN mkdir -p /app/logs/dotnet && chown -R ${APP_UID}:${APP_UID} /app/logs
USER ${APP_UID}

USER root
RUN mkdir -p /app/logs/dotnet /app/cache \
    && chown -R ${APP_UID}:${APP_UID} /app/logs /app/cache
USER ${APP_UID}

# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Linqyard.Api/Linqyard.Api.csproj", "Linqyard.Api/"]
RUN dotnet restore "./Linqyard.Api/Linqyard.Api.csproj"
COPY . .
WORKDIR "/src/Linqyard.Api"
RUN dotnet build "./Linqyard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Linqyard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final
FROM base AS final
# (Re)ensure ownership in final image, then run as app user
ARG APP_UID=64198
USER root
RUN mkdir -p /app/logs/dotnet && chown -R ${APP_UID}:${APP_UID} /app/logs
USER ${APP_UID}
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Linqyard.Api.dll"]
