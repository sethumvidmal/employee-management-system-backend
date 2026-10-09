# ──────────────────────────────────────────────────────────────────────────────
# Stage 1 – Restore & Build
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file first so NuGet restore is cached on its own layer
COPY EmployeeManagement.Api.csproj .
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Publish a Release build into /app/publish
RUN dotnet publish EmployeeManagement.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2 – Runtime (slim ASP.NET image)
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Copy published output from the build stage
COPY --from=build /app/publish .

# Expose HTTP port (HTTPS is handled at the reverse-proxy / load-balancer level)
EXPOSE 8080

# Switch to non-root user
USER appuser

ENTRYPOINT ["dotnet", "EmployeeManagement.Api.dll"]
