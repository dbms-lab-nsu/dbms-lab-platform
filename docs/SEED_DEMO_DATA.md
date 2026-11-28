# Запуск seed-данных

Документ описывает, как заполнить базу данных демонстрационными данными для курса **"Отказоустойчивость и высокодоступность"** и как восстановить их при необходимости.

## Что заполняется

Seed-сервис (`DemoDataSeeder`) создаёт следующую иерархию:

- **Курс** "Отказоустойчивость и высокодоступность" (статус `Published`)
- **Модуль 1** — "Физическая репликация" (`SortOrder = 1`)
  - **Тема 1.1** — "Основы физической репликации" (`SortOrder = 1`)
    - **Урок 1.1.1** — "Что такое физическая репликация и WAL" (`SortOrder = 1`, `IsPublished = true`)
- **Модуль 2** — "Логическая репликация" (`SortOrder = 2`)
  - **Тема 2.1** — "Введение в логическую репликацию" (`SortOrder = 1`)
    - **Урок 2.1.1** — "Отличия логической репликации от физической" (`SortOrder = 1`, `IsPublished = true`)
  - **Тема 2.2** — "Декодирование WAL" (`SortOrder = 2`)
    - **Урок 2.2.1** — "Декодирование WAL" (`SortOrder = 1`, `IsPublished = true`)
  - **Тема 2.3** — "Возможности фильтрации данных" (`SortOrder = 3`)
    - **Урок 2.3.1** — "Подписки и публикации" (`SortOrder = 1`, `IsPublished = true`)
- **Тест** "Логическая репликация: проверка знаний" (привязан к теме 2.1)
  - 1 вопрос с 3 вариантами ответа (1 правильный)

Итого: 1 курс, 2 модуля, 3 темы, 4 урока, 1 тест. Все уроки опубликованы (`IsPublished = true`).

Демонстрационный курс создаёт служебного автора (email `demo-author@dbms-lab.local`) **без пароля** — эта запись не предназначена для входа в систему и не является тестовым пользователем с фиксированным паролем.

## Требования

Перед запуском seed необходимо:

1. Поднять PostgreSQL (через Docker Compose или локально).
2. Применить миграции EF Core к базе данных.
3. Установить `JWT_KEY` (требуется для запуска бэкенда).

## Вариант 1: Локальный запуск через `dotnet run` с авто-сидом

Авто-сид через переменную окружения `SEED_DEMO_DATA=true` работает только в окружении `Development` (`ASPNETCORE_ENVIRONMENT=Development`).
При старте, после применения миграций (сервис `migrations` завершается перед стартом `backend`), выполняется seed. Повторный запуск не создаёт дубликатов.

Из папки бэкенда:

```powershell
cd backend\DBMSLab.Api

# Убедитесь, что ConnectionStrings:Default и Jwt:Key заданы (через user-secrets или environment).
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=dbms_lab;Username=dbms_lab_user;Password=dbms_lab_password"
dotnet user-secrets set "Jwt:Key" "<ваш ключ из .env>"

# Примените миграции:
dotnet ef database update

# Запустите с включённым авто-сидом (только в Development):
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:SEED_DEMO_DATA="true"
dotnet run
```

При старте бэкенд выполнит сидирование и затем поднимет HTTP-сервер.

В окружениях, отличных от `Development`, авто-сид через `SEED_DEMO_DATA` не сработает. Используйте отдельную команду `--seed` (Вариант 3) для явного запуска сидирования.

Также, авто-сидинг работает при запуске через Docker-compose.

Из корня репозитория:

```powershell
# Убедитесь, что JWT_KEY и SEED_DEMO_DATA заполнены в .env, затем поднимите стек:
docker compose up --build -d
```

Проверить результат:

```powershell
# Список опубликованных курсов:
Invoke-RestMethod -Method Get -Uri "http://localhost:8080/api/courses"

# Иерархия курса (используйте реальный Id курса из предыдущего запроса):
Invoke-RestMethod -Method Get -Uri "http://localhost:8080/api/courses/<course-id>/structure"
```

Вывод должен содержать демонстрационный курс и его структуру.

## Вариант 2: Ручной запуск сидинга через отдельную команду `--seed`

Для демонстрационного окружения сидинг можно запустить отдельно, без перезапуска всего бэкенда и без подъёма HTTP-сервера, используя аргумент `--seed`.

**Ограничение:** команда `--seed` работает только в окружении `Development` (`ASPNETCORE_ENVIRONMENT=Development`). В других окружениях она игнорируется.

### PowerShell

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
.\scripts\seed-demo.ps1
```

Или с явным указанием проекта:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
.\scripts\seed-demo.ps1 -BackendProject "backend/DBMSLab.Api/DBMSLab.Api.csproj"
```

### Bash

```bash
export ASPNETCORE_ENVIRONMENT=Development
chmod +x scripts/seed-demo.sh
./scripts/seed-demo.sh
```

Или напрямую через `dotnet`:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project backend/DBMSLab.Api/DBMSLab.Api.csproj -- --seed
```

При этом поднимается только контекст базы данных, выполняется сидирование и процесс завершается — HTTP-сервер не запускается.

## Повторный запуск

Seed является идемпотентным. Перед вставкой проверяется наличие курса по названию "Отказоустойчивость и высокодоступность". Если курс уже существует, сервер завершает заполнение без изменений. Количество демонстрационных курсов не меняется, дубликаты не создаются.

## Восстановление демонстрационных данных

Если данные были удалены или требуется перезаполнить:

1. Удалите демонстрационный курс напрямую в базе (или удалите таблицы и примените миграции заново).
2. Выберите любой вариант запуска сида (Вариант 1 или 2) — seeder создаст курс и все связанные модули, темы, уроки и тест заново.

В Docker Compose:

```powershell
docker compose down
docker compose up --build -d
```

## Проверка через существующие endpoints

После запуска seed:

| Endpoint | Ожидаемый результат |
|---|---|
| `GET /api/courses` | Курс "Отказоустойчивость и высокодоступность" появляется в списке опубликованных курсов |
| `GET /api/courses/{id}/structure` | Возвращает иерархию: модули → темы → уроки с правильным `SortOrder` |
| `GET /api/lessons/{id}` (с JWT) | Авторизованный пользователь получает содержимое опубликованного урока |
| `GET /api/tests/{id}` (с JWT) | Авторизованный пользователь получает тест с вопросами и вариантами ответов |
| `POST /api/tests/{testId}/attempts` (с JWT) | Пользователь может отправить попытку прохождения теста |
| `PUT /api/me/enrollments/{courseId}` (с JWT) | Пользователь может записаться на курс |
| `PUT /api/me/lessons/{lessonId}/progress` (с JWT) | Пользователь может обновить прогресс урока |
| `GET /api/me/courses/{courseId}/progress` (с JWT) | Пользователь может получить прогресс по курсу |
