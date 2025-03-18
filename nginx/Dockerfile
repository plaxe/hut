# Stage 1: Сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Устанавливаем git для клонирования репозитория
RUN apt-get update && apt-get install -y git

# Клонируем репозиторий (замените URL на свой)
RUN git clone https://github.com/plaxe/hut.git .

# Восстанавливаем зависимости и публикуем приложение
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# Stage 2: Runtime-образ
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:5000

# Замените "MyApp.dll" на название вашего сборки
ENTRYPOINT ["dotnet", "WEB.dll"]
