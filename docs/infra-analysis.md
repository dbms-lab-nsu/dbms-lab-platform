https://ai.nsu.ru/issues/18713

# Настройка Docker-инфраструктуры DBMS Lab Platform

Dockerfile and docker-compose Report

# Dockerfile and docker-compose Report

## 1. List of Dockerfile and docker-compose files

### Docker Compose Files
- `docker-compose.yml`

## 2. Details of Dockerfile

### Dockerfile: `docker-compose.yml`
- **Base Image:** `postgres:18.6-alpine`
- **Multi-stage:** No
- **Run as root/non-root:** Root
- **Size of Dependency Layer:** Not provided (requires manual calculation)

## 3. Services

### Services that should work together:
- **Backend (ASP.NET Core)**
- **Frontend (React)**
- **PostgreSQL (Database)**

## 4. Identified Problems

1. **Hardcoded Paths:**
    - `DATABASE_CONNECTION_STRING` in the `docker-compose.yml` file is hardcoded.

2. **Secrets in Code:**
    - Environment variables like `POSTGRES_PASSWORD`, `JWT_KEY` are used in the `docker-compose.yml` file.

3. **Lack of `.dockerignore`:**
    - No `.dockerignore` file is present in the project.

4. **Use of `latest` Tags:**
    - The `postgres:18.6-alpine` image uses the `latest` tag. It is recommended to use a specific version tag.

5. **Lack of Healthcheck:**
    - While healthchecks are defined for `backend` and `frontend` services, `postgres` and `migrate` services do not have healthchecks.

## 5. Table: What Exists / What is Missing by Service

| Service   | Healthcheck | .dockerignore | Secrets in Code | Multi-stage | Base Image               |
|-----------|-------------|---------------|-----------------|-------------|------------------------------|
| Backend   | Yes         | No            | Yes             | No          | `postgres:18.6-alpine`       |
| Frontend  | Yes         | No            | Yes             | No          | `postgres:18.6-alpine`       |
| Postgres  | Yes         | No            | Yes             | No          | `postgres:18.6-alpine`       |
| Migrate   | No          | No            | Yes             | No          | `postgres:18.6-alpine`       |

---

# Dockerfile Templates

## .NET Service Dockerfile Template
```dockerfile
# Use multi-stage builds
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Set the working directory to /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "DBMSLab.Api.dll"]
```


## Frontend Service Dockerfile Template
```dockerfile
# Use multi-stage builds
# Stage 1: Build
FROM node:16.14 AS build
WORKDIR /app

# Copy package.json and package-lock.json and install dependencies
COPY package*.json ./
RUN npm ci

# Copy remaining files and build
COPY . ./
RUN npm run build

# Stage 2: Runtime
FROM nginx:1.21-alpine AS runtime
WORKDIR /usr/share/nginx/html
COPY --from=build /app/dist .

# Expose port 80
EXPOSE 80

# Default command to run
CMD ["nginx", "-g", "daemon off;"]
```


## .dockerignore Template
```.dockerignore (dockerignore)
# Ignore node_modules and other large directories
node_modules
dist
package-lock.json

# Ignore environment files
.env
.env.local
.env.development
.env.test

# Ignore build artifacts
build
out

# Ignore IDE project files
.idea
.vscode

# Ignore temporary files
*.log
*.tmp
```


---

# Reasoning for Base Images and Versions

- **.NET Base Image:** `mcr.microsoft.com/dotnet/aspnet:10.0`
    - This image is the runtime version of .NET Core, which is smaller and optimized for production environments.
    - It provides only the necessary dependencies for running the application, reducing the overall image size.

- **Frontend Base Image:** `node:16.14`
    - This image contains Node.js and npm, which are essential for building and serving the React application.
    - It is a common choice for frontend development, and it includes a minimal set of tools.

- **Postgres Base Image:** `postgres:18.6-alpine`
    - This image is a minimal PostgreSQL image based on Alpine Linux, which is lightweight and suitable for production use.
    - It provides a small footprint while maintaining the necessary functionality for the database.

These choices ensure that the Docker images are optimized for both performance and security, while minimizing the attack surface.



//

# docker-compose.yml

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:18.6-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB:-dbms_lab}
      POSTGRES_USER: ${POSTGRES_USER:-dbms_lab_user}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?Set POSTGRES_PASSWORD}
    ports:
      - "127.0.0.1:${POSTGRES_PORT:-5432}:5432"
    volumes:
      - dbms_lab_postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U \"$${POSTGRES_USER}\" -d \"$${POSTGRES_DB}\""]
      interval: 3s
      timeout: 3s
      retries: 30

  migrate:
    build:
      context: ./backend/DBMSLab.Api
      target: build
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Development}
      DATABASE_CONNECTION_STRING: Host=postgres;Port=5432;Database=${POSTGRES_DB:-dbms_lab};Username=${POSTGRES_USER:-dbms_lab_user};Password=${POSTGRES_PASSWORD:?Set POSTGRES_PASSWORD}
      Jwt__Key: ${JWT_KEY:?Set JWT_KEY (at least 32 bytes)}
    entrypoint: ["/bin/bash", "-ec"]
    command: >-
      (test -x /tmp/ef/dotnet-ef || dotnet tool install dotnet-ef --version 10.0.13 --tool-path /tmp/ef)
      && /tmp/ef/dotnet-ef database update --configuration Release --no-build
    restart: "no"
    depends_on:
      postgres:
        condition: service_healthy

  backend:
    build:
      context: ./backend/DBMSLab.Api
    restart: unless-stopped
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Development}
      DATABASE_CONNECTION_STRING: Host=postgres;Port=5432;Database=${POSTGRES_DB:-dbms_lab};Username=${POSTGRES_USER:-dbms_lab_user};Password=${POSTGRES_PASSWORD:?Set POSTGRES_PASSWORD}
      Jwt__Key: ${JWT_KEY:?Set JWT_KEY (at least 32 bytes)}
      ASPNETCORE_URLS: http://+:8080
    ports:
      - "127.0.0.1:${BACKEND_PORT:-8080}:8080"
    depends_on:
      migrate:
        condition: service_completed_successfully
    healthcheck:
      test: ["CMD-SHELL", "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /api/health/db HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3 && read -r response <&3 && [[ \"$$response\" == *' 200 '* ]] || exit 1"]
      interval: 5s
      timeout: 5s
      start_period: 10s
      retries: 20

  frontend:
    profiles: ["frontend"]
    build:
      context: ./frontend
    restart: unless-stopped
    ports:
      - "127.0.0.1:${FRONTEND_PORT:-3000}:3000"
    depends_on:
      backend:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "node", "-e", "fetch('http://127.0.0.1:3000').then(r=>process.exit(r.ok?0:1)).catch(()=>process.exit(1))"]
      interval: 5s
      timeout: 5s
      start_period: 20s
      retries: 20

volumes:
  dbms_lab_postgres_data:
```


# Commands for Running/Stopping/Rebuilding a Single Service

1. **Start all services (including frontend for development):**
```shell script
docker compose up -d
```


2. **Start only the backend service:**
```shell script
docker compose up --build -d backend
```


3. **Start only the frontend service:**
```shell script
docker compose up --build -d frontend
```


4. **Stop all services:**
```shell script
docker compose down
```


5. **Stop only the backend service:**
```shell script
docker compose stop backend
```


6. **Stop only the frontend service:**
```shell script
docker compose stop frontend
```


7. **Rebuild and restart the backend service:**
```shell script
docker compose up --build -d --no-deps --force-recreate --wait --wait-timeout 120 backend
```


8. **Rebuild and restart the frontend service:**
```shell script
docker compose up --build -d --no-deps --force-recreate --wait --wait-timeout 120 frontend
```


These commands provide a structured way to manage the services within your project, allowing you to easily start, stop, and rebuild individual services as needed.


## Управление конфигами

### .env.example

```
# Compose substitutions; only environment: entries are passed to processes.
# Local demonstration only. Replace password and key before use.
POSTGRES_DB=dbms_lab
POSTGRES_USER=dbms_lab_user
POSTGRES_PASSWORD=replace_with_local_password
# Host ports, bound to loopback. Container ports remain 5432/8080/3000.
POSTGRES_PORT=5432
BACKEND_PORT=8080
FRONTEND_PORT=3000
# Read by ASP.NET; Development enables /openapi/v1.json.
ASPNETCORE_ENVIRONMENT=Development
# Generate locally: openssl rand -hex 32 (at least 32 UTF-8 bytes).
JWT_KEY=
# DATABASE_CONNECTION_STRING is assembled in Compose from POSTGRES_*.
# Frontend currently builds with http://localhost:8080; see docs/DOCKER.md.
DATABASE_CONNECTION_STRING=Server=postgres;Port=5432;Database=dbms_lab;User Id=dbms_lab_user;Password=replace_with_local_password;
```


### .gitignore

```
.env
```


### Проброс конфигурации в .NET-приложение

1. **Через переменные окружения в docker-compose (environment: секция)**:
   В `docker-compose.yml` для сервиса `backend` используйте `environment` секцию для передачи переменных окружения:
```yaml
backend:
     build:
       context: ./backend/DBMSLab.Api
     restart: unless-stopped
     environment:
       ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Development}
       DATABASE_CONNECTION_STRING: "Server=postgres;Port=5432;Database=dbms_lab;User Id=dbms_lab_user;Password=${POSTGRES_PASSWORD};"
       Jwt__Key: ${JWT_KEY}
       ASPNETCORE_URLS: http://+:8080
     ports:
       - "127.0.0.1:${BACKEND_PORT:-8080}:8080"
     depends_on:
       migrate:
         condition: service_completed_successfully
     healthcheck:
       test:
         - CMD
         - /bin/bash
         - -ec
         - >-
           exec 3<>/dev/tcp/127.0.0.1/8080;
           printf 'GET /api/health/db HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n' >&3;
           read -r response <&3;
           [[ "$$response" == *' 200 '* ]]
       interval: 5s
       timeout: 5s
       start_period: 10s
       retries: 20
```


2. **Или через appsettings.Production.json с volume**:
    - Создайте файл `appsettings.Production.json` в проекте backend.
    - Добавьте в него конфигурацию:
```json
{
       "ConnectionStrings": {
         "DefaultConnection": "Server=postgres;Port=5432;Database=dbms_lab;User Id=dbms_lab_user;Password=${POSTGRES_PASSWORD};"
       },
       "Jwt": {
         "Key": "${JWT_KEY}"
       }
     }
```

- В `docker-compose.yml` используйте volume для монтирования `appsettings.Production.json`:
```yaml
backend:
       build:
         context: ./backend/DBMSLab.Api
       restart: unless-stopped
       environment:
         ASPNETCORE_ENVIRONMENT: Production
       ports:
         - "127.0.0.1:${BACKEND_PORT:-8080}:8080"
       volumes:
         - ./backend/DBMSLab.Api/appsettings.Production.json:/app/appsettings.Production.json
       depends_on:
         migrate:
           condition: service_completed_successfully
       healthcheck:
         test:
           - CMD
           - /bin/bash
           - -ec
           - >-
             exec 3<>/dev/tcp/127.0.0.1/8080;
             printf 'GET /api/health/db HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n' >&3;
             read -r response <&3;
             [[ "$$response" == *' 200 '* ]]
         interval: 5s
         timeout: 5s
         start_period: 10s
         retries: 20
```


### Инструкция "первый запуск" (5 шагов)

1. **Клонирование**:
```textmate
git clone https://github.com/your-repo/dbms-lab-platform.git
   cd dbms-lab-platform
```


2. **Копирование .env.example → .env**:
```textmate
cp .env.example .env
```


3. **Заполнение переменных**:
   Откройте `.env` файл и заполните необходимые переменные.

4. **docker compose up -d**:
```textmate
docker compose up -d
```


5. **Проверка работоспособности**:
```textmate
docker compose logs -f backend
```

Ожидайте, пока сервис backend будет доступен и здоров. Вы можете проверить его работоспособность, открыв браузер и посетив `http://localhost:8080/api/health`.

Этот процесс обеспечит правильную настройку и запуск всех сервисов в Docker-окружении.


