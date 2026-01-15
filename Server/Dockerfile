# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY Server/Server.csproj Server/
RUN dotnet restore Server/Server.csproj

# Copy source code
COPY Server/ Server/
WORKDIR /src/Server
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create directories for data and logs
RUN mkdir -p /app/data /app/logs

# Copy published files
COPY --from=publish /app/publish .

EXPOSE 5247

ENTRYPOINT ["dotnet", "Server.dll"]

