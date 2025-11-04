# Multi-stage Dockerfile for Time Reporting MCP WebSocket Server

#######################
# Stage 1: Build Stage
#######################
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files for dependency restoration
# Copy in order of least to most frequently changed for better layer caching
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["TimeReportingAnalyzers/TimeReportingAnalyzers.csproj", "TimeReportingAnalyzers/"]
COPY ["TimeReportingMcp.WebSocket/TimeReportingMcp.WebSocket.csproj", "TimeReportingMcp.WebSocket/"]

# Restore dependencies (this layer will be cached unless project files change)
RUN dotnet restore "TimeReportingMcp.WebSocket/TimeReportingMcp.WebSocket.csproj"

# Copy remaining source code
COPY ["TimeReportingAnalyzers/", "TimeReportingAnalyzers/"]
COPY ["TimeReportingMcp.WebSocket/", "TimeReportingMcp.WebSocket/"]

# Build the application in Release configuration
WORKDIR /src/TimeReportingMcp.WebSocket
RUN dotnet build "TimeReportingMcp.WebSocket.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "TimeReportingMcp.WebSocket.csproj" -c Release -o /app/publish /p:UseAppHost=false

########################
# Stage 2: Runtime Stage
########################
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user for running the application
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application from build stage
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port 5002 for WebSocket MCP server
EXPOSE 5002

# Health check using the /health endpoint
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:5002/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5002
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "TimeReportingMcp.WebSocket.dll"]
