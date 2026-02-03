# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file
COPY ["distels.csproj", "./"]
RUN dotnet restore "distels.csproj"

# Copy everything else
COPY . .
RUN dotnet build "distels.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "distels.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# Start the app
ENTRYPOINT ["dotnet", "distels.dll"] 
