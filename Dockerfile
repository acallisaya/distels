# Build stage - PRIMERO definimos 'build'
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY ["distels.csproj", "./"]
RUN dotnet restore "distels.csproj"

# Copiar todo el código
COPY . .
WORKDIR "/src"

# Publicar - ESTO CREA la carpeta /app/publish
RUN dotnet publish "distels.csproj" -c Release -o /app/publish

# Runtime stage - AHORA SÍ existe 'publish'
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instalar dependencias necesarias
RUN apt-get update && apt-get install -y \
    curl \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

# Crear directorios
RUN mkdir -p wwwroot/uploads/lotes

# ✅ AHORA SÍ - Copiar desde la etapa 'build' que creó /app/publish
COPY --from=build /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:${PORT:-8080}/health || exit 1

# Puerto
EXPOSE 8080

# Iniciar
ENTRYPOINT ["dotnet", "distels.dll"]