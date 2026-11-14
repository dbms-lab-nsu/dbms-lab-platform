# Локальная разработка

## Предварительные требования
- Docker Desktop (Windows, macOS, Linux)
- .NET SDK 10.0 LTS
- Node.js (если используется фронтенд)

## Быстрый старт
1. Клонируйте репозиторий:
```textmate
git clone https://github.com/your-repo/dbms-lab-platform.git
   cd dbms-lab-platform
```


2. Создайте и настройте файл `.env`:
```textmate
cp .env.example .env
```

Заполните переменные `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `POSTGRES_PORT`, `BACKEND_PORT`, `FRONTEND_PORT`, `ASPNETCORE_ENVIRONMENT`, и `JWT_KEY` в файле `.env`.

3. Запустите проект:
```textmate
docker compose up -d
```


## Основные команды
- Запуск:
```textmate
docker compose up -d
```


- Остановка:
```textmate
docker compose down
```


- Пересборка сервиса (backend):
```textmate
docker compose up -d --build backend
```


- Логи всех сервисов:
```textmate
docker compose logs -f
```


- Логи конкретного сервиса (backend):
```textmate
docker compose logs -f backend
```


- Статус всех сервисов:
```textmate
docker compose ps
```


## Диагностика
- Проверка статуса:
```textmate
docker compose ps
```

Все сервисы должны быть в состоянии "Up".

- Зайти в контейнер backend:
```textmate
docker compose exec backend sh
```


- Подключиться к Postgres:
    - Из контейнера:
```textmate
docker compose exec postgres psql -U dbms_lab_user -d dbms_lab
```

- С хоста:
```textmate
psql -h localhost -p 5432 -U dbms_lab_user -d dbms_lab
```


- Типовые ошибки:
    - **Порт занят**: используйте `lsof -i :5000` и устраните конфликт или смените порт в `.env`.
    - **Healthcheck не проходит**: проверьте логи сервиса с `docker compose logs backend`.
    - **Старые данные в volume**: удаляйте volume с `docker compose down -v` (осторожно!).

## Структура проекта (кратко)
- `services/backend/` — .NET API
- `services/frontend/` — фронтенд
- `docker-compose.yml` — оркестрация