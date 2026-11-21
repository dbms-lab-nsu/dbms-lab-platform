# Repository Guidelines

## Project Structure & Module Organization

DBMS Lab is an educational platform for exploring PostgreSQL internals.
- `backend/DBMSLab.Api/`: ASP.NET Core API; `Auth/` handles JWT authentication, `Contracts/` defines request/response types, `Domain/` holds entities, `Data/` contains the EF Core context, and `Migrations/` tracks schema changes.
- `frontend/src/app/`: Next.js App Router pages, layouts, and styles; static assets live in `frontend/public/`.
- `tests/api/`: Python API smoke tests; `scripts/test-auth.ps1` checks authentication.
- `docs/PHASE1_SETUP.md`: database and authentication setup; `docker-compose.yml` defines the local stack.

## Build, Test, and Development Commands

Run from the repository root unless noted:
- Copy `.env.example` to `.env` and set `JWT_KEY` before starting services.
- `docker compose up --build -d`: build and start PostgreSQL, backend, and frontend (default ports 5432, 8080, and 3000).
- `dotnet build backend/DBMSLab.Api/DBMSLab.Api.csproj`: compile the API using the SDK selected by `global.json`.
- In `frontend/`, run `npm ci`, then `npm run dev` for development, `npm run build` for production compilation, or `npm run lint` for ESLint.
- In `tests/`, run `uv run pytest -v` against a running backend.
- `pwsh -File scripts/test-auth.ps1`: verify registration, login, and the Student role; creates a test user.

## Coding Style & Naming Conventions

Match existing styles: four-space indentation in C# and Python; two spaces in TypeScript/TSX. Use PascalCase for C# types and members, camelCase for local variables, and `test_*.py` files with `test_*` functions for pytest. Keep TypeScript strict checks enabled and use Next.js ESLint rules. Ruff is available in the test dependencies. Follow `frontend/AGENTS.md`, including its local Next.js documentation instructions, for frontend changes.

## Testing Guidelines

API tests use pytest and httpx. Override the target with `API_BASE_URL`; the default is `http://localhost:8080`. Add regression checks for changed API behavior. Pull-request CI starts PostgreSQL and the backend, then runs pytest. No coverage threshold is configured.

## Commit & Pull Request Guidelines

Recent commits use short, imperative subjects, including `test: initialize API smoke tests` and `ci: run API smoke tests on pull requests`. Use a relevant prefix when helpful. Describe behavior changes, reference related issues, report validation commands, and include screenshots for UI changes.

## Security & Configuration

Keep secrets in `.env` or .NET user-secrets; never commit credentials. Commit EF Core migrations with schema changes. Preserve PostgreSQL volumes during routine development.
