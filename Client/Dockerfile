FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем проект и восстанавливаем зависимости
COPY Client/Client.csproj Client/
RUN dotnet restore Client/Client.csproj

# Копируем остальной код
COPY Client/ Client/

# Публикуем в Release
RUN dotnet publish Client/Client.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

# URL API сервера (по умолчанию на сервис server в docker-compose)
ENV SERVER_URL=http://server:5247

COPY --from=build /app/publish .

# Консольный клиент (интерактивный)
ENTRYPOINT ["dotnet", "Client.dll"]

