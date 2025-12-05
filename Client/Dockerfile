# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY Client/Client.csproj Client/
RUN dotnet restore Client/Client.csproj

# Copy source code
COPY Client/ Client/
WORKDIR /src/Client
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Client.dll"]

