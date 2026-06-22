# syntax=docker/dockerfile:1

# ───────────────────────── build (Blazor WASM → статика) ─────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только манифесты — слой restore кэшируется, пока не менялись .csproj/props.
COPY ["Directory.Build.props", "Directory.Packages.props", "NuGet.config", "./"]
COPY ["WorkflowEditor.Core/WorkflowEditor.Core.csproj", "WorkflowEditor.Core/"]
COPY ["WorkflowEditor.Client/WorkflowEditor.Client.csproj", "WorkflowEditor.Client/"]
RUN dotnet restore "WorkflowEditor.Client/WorkflowEditor.Client.csproj"

# Затем исходники + публикация статического сайта в /app/publish/wwwroot.
COPY . .
RUN dotnet publish "WorkflowEditor.Client/WorkflowEditor.Client.csproj" \
        -c Release --no-restore -o /app/publish

# ───────────────────────── runtime (nginx отдаёт статику) ─────────────────────────
FROM nginx:alpine AS runtime

# Свой server-блок: SPA-fallback, gzip, no-cache для index.html.
COPY WorkflowEditor.Client/nginx.conf /etc/nginx/conf.d/default.conf

# Только содержимое wwwroot — это и есть весь клиент.
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget -q --spider http://localhost:8080/ || exit 1

# Базовый образ nginx сам запускает сервер в foreground.
