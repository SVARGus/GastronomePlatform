# syntax=docker/dockerfile:1
# Прод-образ GastronomePlatform: SPA (Vite) + WebAPI (.NET 8) в одном контейнере.
# Контекст сборки — корень репозитория:
#   docker compose -f docker-compose.prod.yml build
#   (или: docker build -f src/GastronomePlatform.WebAPI/Dockerfile .)

# === Стадия 1: сборка SPA ===
FROM node:22-alpine AS web
WORKDIR /web
COPY src/GastronomePlatform.Web/package*.json ./
RUN npm ci --no-fund --no-audit
COPY src/GastronomePlatform.Web/ ./
# outDir переопределяется: в конфиге он указывает на wwwroot соседнего проекта,
# которого в этой стадии нет. Результат забирает финальная стадия.
RUN npx tsc -b && npx vite build --outDir dist --emptyOutDir

# === Стадия 2: публикация WebAPI ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# .editorconfig обязателен: он приглушает часть диагностик (например, CS1591
# на инфраструктурных членах); без него TreatWarningsAsErrors валит publish.
COPY Directory.Build.props .editorconfig ./
COPY src/Common/ src/Common/
COPY src/Modules/ src/Modules/
COPY src/GastronomePlatform.WebAPI/ src/GastronomePlatform.WebAPI/
RUN dotnet publish src/GastronomePlatform.WebAPI/GastronomePlatform.WebAPI.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# === Стадия 3: рантайм ===
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# curl — для docker-healthcheck (в базовом образе его нет)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish ./
# SPA поверх publish: wwwroot из dev-сборки исключён .dockerignore,
# сюда попадает только свежая сборка стадии web.
COPY --from=web /web/dist ./wwwroot

# Медиафайлы — на volume (см. docker-compose.prod.yml)
VOLUME ["/data/media"]

# Порт задаёт базовый образ (ASPNETCORE_HTTP_PORTS=8080); дублирование через
# ASPNETCORE_URLS вызывало warning «Overriding HTTP_PORTS» при старте.
EXPOSE 8080

ENTRYPOINT ["dotnet", "GastronomePlatform.WebAPI.dll"]
