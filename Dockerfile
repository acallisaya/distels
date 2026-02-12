# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
# NO definir ASPNETCORE_URLS - Render usa PORT

# Expose port (Render ignora esto)
EXPOSE 8080

# Start the app
ENTRYPOINT ["dotnet", "distels.dll"]