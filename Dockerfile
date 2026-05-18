# Stage 1: Build & Compile (SDK Layer)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Stage 2: Runtime Execution (Hardened Security Layer)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy the published output from build-env
COPY --from=build-env /app/out .

# DevSecOps Hardening: Create non-privileged system user, change ownership, and switch context
RUN useradd -u 10001 -m appuser && \
    chown -R appuser:appuser /app

USER appuser

# Expose non-root ports
EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "AttendenceManagementSystem.dll"]
