# API tests

The suite uses pytest and httpx. Install uv and Python 3.14 or newer.
Use the existing repository .env with JWT_KEY set. Start the ordinary stack from
the repository root:

```bash
docker compose up -d --build postgres backend
```

Wait until `http://localhost:8080/api/health/db` returns 200, then run from `tests/`:

```bash
uv run --env-file ../.env pytest -v
```

Pytest uses the running backend and PostgreSQL from docker-compose.yml.
It does not start or stop containers or remove database volumes.

The default run includes functional tests and JWT checks that do not require
access to the backend signing key. The four `jwt_signing` cases are deselected
and reported in the pytest summary. Tests marked `perfomance` are also excluded
by default.

## Select a suite

```bash
# Health, registration, login, and protected endpoint tests
uv run --env-file ../.env pytest -v -m "not jwt and not perfomance"

# JWT checks that do not require the signing key
uv run --env-file ../.env pytest -v -m jwt

# All JWT checks, including correctly signed invalid claims
uv run --env-file ../.env pytest -v -m jwt --run-jwt-signing

# Functional and JWT suites, including signing-key cases
uv run --env-file ../.env pytest -v --run-jwt-signing
```

`--run-jwt-signing` enables tests requiring `JWT_KEY` in the pytest process
environment. It must match the key used by the running test backend.
The env file is loaded explicitly by uv; pytest does not load it automatically.
A missing or incorrect key causes an error or failure when these tests are enabled.
These tests currently assume HS256 and a one-hour token lifetime.

## Target and test data

API_BASE_URL defaults to http://localhost:8080. Set it in the environment if
the running backend uses another address. SQL fixtures execute psql in this
project's Compose postgres service, using POSTGRES_USER and POSTGRES_DB from
the container. The backend must use that same database.

There is no separate test Compose file or TEST_API_EXTERNAL setting.
The suite does not require a special database name or port.

Tests create users with UUID-based email addresses to avoid conflicts between
runs. Generated users and courses remain in PostgreSQL after execution.

## Perfomance tests

The marker is spelled `perfomance`. Run this suite separately:

```bash
uv run --env-file ../.env pytest -v -m perfomance
```

The concurrent registration test starts four requests for one unique email,
using different names and passwords. It expects one `201` and three `409`
responses, verifies the winning account, and rejects the losing passwords.
The client synchronizes request starts but cannot force a particular database
interleaving. A passing run does not prove the absence of every race condition.
This test checks correctness under concurrency; it does not measure throughput.

To include every suite, override the default marker expression:

```bash
uv run --env-file ../.env pytest -v -m "" --run-jwt-signing
```

## CI

Every pull request and manual workflow run starts two independent matrix jobs:

- `API / functional`: health, registration, login, author courses, enrollments,
  progress, and protected endpoint tests, including progress concurrency checks.
- `API / jwt`: all JWT tests, including the four signing-key cases.

Each job starts PostgreSQL and backend using the ordinary docker-compose.yml
on its own runner and generates one signing key shared by Compose and pytest.
The workflow owns startup and cleanup; pytest uses the running services.
Dependencies are installed from `uv.lock` using `--frozen`.

Test results appear in the workflow output. Each job uploads Compose logs on failure.
Artifacts are retained for seven days. The suites do not include load tests.

The separate `API / perfomance` job runs only when a workflow is started manually
with `run_perfomance` enabled. It does not run on pull requests and should not
be configured as a required branch-protection check. It uses its own stack and
uploads its own failure logs. A failure is visible and fails the
manually selected workflow run.

## Author course tests

These tests cover course creation and access checks for the author course API.

The repository .env.example enables TEST_PROVISION_AUTHOR=1.
With this setting, the author fixture registers a unique user through the API
and assigns Author through psql in the same Docker Compose postgres service.
The default Student role is retained. Setup errors fail the test.
If your existing .env lacks this setting, add TEST_PROVISION_AUTHOR=1.

Local runs and CI load .env through uv; pytest does not load it itself.
Alternatively, set both TEST_AUTHOR_EMAIL and TEST_AUTHOR_PASSWORD for an existing
Author in this same database; these take precedence over automatic provisioning.
The fixture obtains the account ID and JWT through login and profile requests.

Run from tests/:

```bash
uv run --env-file ../.env pytest -v api/test_author_courses.py
```

Generated authors and courses remain in PostgreSQL after execution.
Courses use unique titles.
The creation test expects a JSON array from GET /api/author/courses containing
full course objects without pagination, including Draft courses.

## Enrollment and progress tests

These tests use real HTTP requests against the running backend and its PostgreSQL.
Run from `tests/` using the existing repository .env:

```bash
uv run --env-file ../.env pytest -v api/test_enrollments_progress.py
```

The learning_db fixture executes SQL in the ordinary Compose postgres service.
It prepares course content and checks stored rows. Enrollment and progress
operations themselves are performed through the backend API.
JWT_KEY is needed in pytest only when --run-jwt-signing is enabled.

The fixtures reuse api_client, user_data, registered_user, and access_token.
Additional users are registered and authenticated through the API. SQL assigns
Author or Administrator before login when a role test needs it; the profile API
confirms the role. The course owner has a fresh Author token, and archive scenarios
use the author API. SQL creates unique course, module, topic, and lesson UUIDs
because module and lesson authoring endpoints are not available.
Each test gets its own data: three published lessons and one unpublished lesson
by default. SQL setup is transactional.
Failures in setup fail the tests; no cases are silently skipped.

Generated data remains after execution. Tests never use fixed user IDs,
and repeated runs create independent users and courses.
SQL reads also verify persistence and the absence of duplicate rows.

Coverage includes:

- Enrollment, repeated enrollment, Draft/Archived/missing courses, and sorted
  user-specific course lists.
- All three lesson statuses, repeated completion, resetting completion, and
  percentages 0/33/67/100. Repeating a status preserves updatedAt in both the
  response and database, including concurrent identical updates.
- Exclusion of stored progress for unpublished lessons, publication changes,
  and courses with no published lessons.
- Missing JWT, missing enrollment, missing lesson, invalid or missing status,
  and isolation between two users.
- Rejection of numeric enum values, numeric strings, booleans, arrays, and extra
  fields; case-insensitive named inputs return canonical status names.
- ProblemDetails content type, status, title, traceId, and action error instance;
  automatic validation errors contain a nonempty errors dictionary. JWT refusals
  require a Bearer challenge without requiring a JSON response body.
- Forbidden writes to hidden lessons and archived courses, with unchanged data.
  Archived enrollment history and progress remain readable.
- Author and Administrator preview privileges do not bypass enrollment or
  publication requirements for learning updates.
- Concurrent enrollment, first and repeated progress writes, different lessons,
  different statuses for one lesson, and enrollment racing with progress.
  Different-status requests must all succeed; the database determines the final
  status and timestamp, and the course summary must agree with that state.
  Only enrollment racing with progress permits an initial 403 before enrollment
  commits; requests after enrollment must succeed.
- Course structure, lesson reads, completion, and persistence after logging in
  again. This is an API regression scenario; the frontend course page is still
  a placeholder, so actual frontend integration remains to be tested later.

Concurrency cases run in the functional PR job, without the perfomance marker.
A client barrier synchronizes request starts but cannot force database scheduling.
The backend uses upserts and required named-status validation. The tests guard
these contracts without xfail. Draft and Archived enrollments must return 403,
as specified in ../docs/ENROLLMENTS_PROGRESS_API.md.

CI uses the ordinary Compose stack for every job, preserves failure logs through
the existing artifact upload, and stops the stack afterwards.
