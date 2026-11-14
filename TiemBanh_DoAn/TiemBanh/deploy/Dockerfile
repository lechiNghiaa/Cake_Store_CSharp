# syntax=docker/dockerfile:1
# ==================== TRIỂN KHAI — MULTI-STAGE BUILD .NET 9 ====================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Directory.Build.props global.json TiemBanh.sln ./
COPY src/ ./src/
COPY tests/ ./tests/
# Restore — proxy CA là secret build tùy chọn, không được copy vào image.
RUN --mount=type=secret,id=proxy_ca \
    if [ -f /run/secrets/proxy_ca ]; then \
      SSL_CERT_FILE=/run/secrets/proxy_ca dotnet restore TiemBanh.sln; \
    else dotnet restore TiemBanh.sln; fi
RUN dotnet publish src/TiemBanh.Api -c Release --no-restore -o /out/api && \
    dotnet publish src/TiemBanh.Web -c Release --no-restore -o /out/web && \
    dotnet publish src/TiemBanh.Migrator -c Release --no-restore -o /out/migrate
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS migrate
WORKDIR /app
COPY --from=build /out/migrate/ ./
USER $APP_UID
ENTRYPOINT ["dotnet", "TiemBanh.Migrator.dll"]
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS api
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /out/api/ ./
RUN mkdir -p /app/uploads && chown -R $APP_UID:$APP_UID /app/uploads
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TiemBanh.Api.dll"]
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS web
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /out/web/ ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TiemBanh.Web.dll"]
