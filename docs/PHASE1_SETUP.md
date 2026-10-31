# DBMS Lab — этап 1: Backend Foundation

Это **дополнение** к ранее созданному проекту, а не новый репозиторий. Скопируйте содержимое архива в корень `dbms-lab-platform`. Оно заменит только `backend/DBMSLab.Api/Program.cs` и добавит новые файлы в `backend/DBMSLab.Api/`, `docs/` и `scripts/`.

Перед заменой `Program.cs` сохраните свою версию через Git (коммит или `git diff`). Не удаляйте PostgreSQL Docker volume: миграции применяются поверх текущей БД.

## 1. NuGet-пакеты

Откройте PowerShell **из корня** `dbms-lab-platform` и выполните:

```powershell
cd backend\DBMSLab.Api

dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.3
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.13
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.13
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.13

dotnet build
```

Сохраните изменения `DBMSLab.Api.csproj` в Git. Уже существующий пакет `Microsoft.AspNetCore.OpenApi` из стандартного шаблона должен остаться.

## 2. Локальные настройки (для `dotnet run` и `dotnet ef`)

В `backend/DBMSLab.Api`:

```powershell
dotnet user-secrets init
# Подставьте пароль из вашего корневого .env (POSTGRES_PASSWORD), если он отличается:
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=dbms_lab;Username=dbms_lab_user;Password=dbms_lab_password"

$bytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:Key" $jwtKey
# Показать значение, чтобы добавить его в корневой .env:
$jwtKey
```

Добавьте в существующий **корневой** `.env` (если файла нет, скопируйте `.env.example`):

```dotenv
JWT_KEY=сюда_вставьте_сгенерированное_значение
```

Добавьте в **уже существующий** `docker-compose.yml` в секцию `services.backend.environment` ОДНУ строку (с тем же отступом, что `ASPNETCORE_ENVIRONMENT`):

```yaml
Jwt__Key: ${JWT_KEY:?Set JWT_KEY in .env}
```

> Не добавляйте ещё один блок `environment`, а дополните существующий. Не коммитьте `.env` и реальные секреты. `DATABASE_CONNECTION_STRING` из нулевого этапа остаётся без изменений: приложение понимает его и `ConnectionStrings__Default`.

Дополните `.env.example` строкой `JWT_KEY=` **без настоящего ключа**.

Если предыдущий `docker compose` сейчас запущен, новый `Program.cs` не повлияет на него до пересборки.

## 3. Миграция

Для EF Core установите инструмент (если `dotnet ef` уже есть, используйте `dotnet tool update`):

```powershell
dotnet tool install --global dotnet-ef --version 10.0.13
# если уже установлен:
# dotnet tool update --global dotnet-ef --version 10.0.13
```

Поднимите только PostgreSQL из КОРНЯ репозитория:

```powershell
cd ..\..
docker compose up -d postgres
```

Вернитесь в backend и создайте миграцию:

```powershell
cd backend\DBMSLab.Api
dotnet ef migrations add InitialIdentityAndContent
dotnet ef database update
```

Команда `migrations add` СОЗДАЁТ исходные файлы миграции в папке `Migrations` — их нужно коммитить. `database update` применяет схему к работающей БД.

Если `database update` сообщает об ошибке подключения, проверьте, что контейнер `postgres` healthy, порт `5432` доступен и пароль в user-secrets совпадает с реально инициализированным PostgreSQL volume. Смена `POSTGRES_PASSWORD` в `.env` для уже инициализированного volume не меняет пароль существующего пользователя.

## 4. Запуск

Из КОРНЯ проекта:

```powershell
cd ..\..
docker compose up --build -d
docker compose ps
```

Проверка endpoints:

- `http://localhost:8080/api/health` — backend живой;
- `http://localhost:8080/api/health/db` — backend может подключиться к PostgreSQL;
- `http://localhost:8080/openapi/v1.json` — OpenAPI, если Development.

Созданная миграция также должна создать таблицы `users`, `roles`, `user_roles`, `courses`, `modules`, `topics`, `lessons` и вспомогательные таблицы Identity.

## 5. Проверка регистрации и JWT

Из корня репозитория:

```powershell
.\scripts\test-auth.ps1
```

Если PowerShell запрещает запуск скриптов, используйте одноразовый вызов без смены системных настроек:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-auth.ps1
```

Скрипт автоматически регистрирует уникального тестового пользователя, выполняет вход, вызывает `/api/auth/me` и проверяет роль `Student`.

Без токена `GET /api/auth/me` должен вернуть HTTP 401. Неверный пароль на `POST /api/auth/login` должен вернуть HTTP 401.

## 6. Что пока намеренно не делаем

- подтверждение email и восстановление пароля;
- refresh-токены, отзыв токенов и ограничение попыток входа;
- интерфейс frontend для регистрации;
- управление ролями администратором;
- CRUD курсов/модулей/тем/уроков;
- курсовые прогрессы, тесты, лабораторные работы.

Эти функции потребуют отдельного проектирования и тестов, прежде чем использовать систему в публичном развёртывании. JWT-ключ нужно хранить вне репозитория, HTTPS обязательно при публичной эксплуатации.

## 7. Проверка перед коммитом

```powershell
cd backend\DBMSLab.Api
dotnet build
# Затем из корня:
cd ..\..
git status
git add backend/DBMSLab.Api docs/PHASE1_SETUP.md scripts/test-auth.ps1 docker-compose.yml .env.example
git commit -m "Add EF Core schema and JWT authentication"
```

Проверьте `git status`, чтобы случайно не добавить `.env` или файлы с секретами.
