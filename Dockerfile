FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish ConsumersVoiceSystemPrototype.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos '' appuser && mkdir -p /app/wwwroot/uploads && chown -R appuser:appuser /app

COPY --from=build --chown=appuser:appuser /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV USE_SQLITE=1

EXPOSE 8080

USER appuser

VOLUME ["/app/app.db", "/app/wwwroot/uploads"]

HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD dotnet ConsumersVoiceSystemPrototype.dll --urls http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ConsumersVoiceSystemPrototype.dll"]
