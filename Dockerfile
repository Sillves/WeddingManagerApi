FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Install Scaleway CLI
RUN apt-get update && apt-get install -y curl && \
    curl -s https://raw.githubusercontent.com/scaleway/scaleway-cli/master/scripts/install.sh | bash && \
    rm -rf /var/lib/apt/lists/*

COPY ["WeddingManager.Web/WeddingManager.Web.csproj", "WeddingManager.Web/"]
COPY ["WeddingManager.Infrastructure/WeddingManager.Infrastructure.csproj", "WeddingManager.Infrastructure/"]
COPY ["WeddingManager.Application/WeddingManager.Application.csproj", "WeddingManager.Application/"]
COPY ["WeddingManager.Domain/WeddingManager.Domain.csproj", "WeddingManager.Domain/"]
RUN dotnet restore "WeddingManager.Web/WeddingManager.Web.csproj"
COPY . .
WORKDIR "/src/WeddingManager.Web"
RUN dotnet build "WeddingManager.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WeddingManager.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Install Scaleway CLI in runtime
RUN apt-get update && apt-get install -y curl && \
    curl -s https://raw.githubusercontent.com/scaleway/scaleway-cli/master/scripts/install.sh | bash && \
    rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeddingManager.Web.dll"]