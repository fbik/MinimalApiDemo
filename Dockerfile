# Используем официальный образ .NET
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проекта и восстанавливаем зависимости
COPY MinimalApiDemo.csproj .
RUN dotnet restore

# Копируем весь код и собираем приложение
COPY . .
RUN dotnet publish -c release -o /app

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# Открываем порт
EXPOSE 8080
EXPOSE 8081

# Запускаем приложение
ENTRYPOINT ["dotnet", "MinimalApiDemo.dll"]
