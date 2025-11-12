
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY MinimalApiDemo.csproj .
RUN dotnet restore


COPY . .
RUN dotnet publish -c release -o /app


FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# Копируем фронтенд
COPY --from=build /src/wwwroot ./wwwroot/

EXPOSE 8080
ENTRYPOINT ["dotnet", "MinimalApiDemo.dll"]

