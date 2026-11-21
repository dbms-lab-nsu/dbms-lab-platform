# API tests

The suite uses pytest and httpx. Install uv and Python 3.14 or newer.
Start PostgreSQL and the backend using the repository setup instructions in
`../docs/PHASE1_SETUP.md` before running tests.

Run the following commands from `tests/`:

```bash
uv sync
uv run --env-file ../.env pytest -v
```

The default run includes functional tests and JWT checks that do not require
access to the backend signing key. The four `jwt_signing` cases are deselected
and reported in the pytest summary. Tests marked `perfomance` are also excluded
by default.

## Select a suite

```bash
# Health, registration, login, and protected endpoint tests
uv run --env-file ../.env pytest -v -m "not jwt and not perfomance"

# JWT checks that do not require the signing key
uv run pytest -v -m jwt

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

`API_BASE_URL` defaults to `http://localhost:8080`. To use another test backend:

```bash
API_BASE_URL=http://localhost:8081 uv run pytest -v
```

Tests create users with UUID-based email addresses to avoid conflicts between
runs. Created users remain in the test database; the suite does not delete them.

## Perfomance tests

The marker is spelled `perfomance`. Run this suite separately:

```bash
uv run pytest -v -m perfomance
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

- `API / functional`: health, registration, login, and protected endpoint tests.
- `API / jwt`: all JWT tests, including the four signing-key cases.

Each job starts its own PostgreSQL and backend and generates one signing key
shared by Compose and pytest. The functional job also verifies the schema and
migration rerun. Dependencies are installed from `uv.lock` using `--frozen`.

Each job uploads a JUnit XML report when available and Compose logs on failure.
Artifacts are retained for seven days. The suites do not include load tests.

The separate `API / perfomance` job runs only when a workflow is started manually
with `run_perfomance` enabled. It does not run on pull requests and should not
be configured as a required branch-protection check. It uses its own stack and
uploads its own report and failure logs. A failure is visible and fails the
manually selected workflow run.

## Author course tests

These tests cover course creation and access checks for the author course API.

The repository .env.example enables TEST_PROVISION_AUTHOR=1. With this setting,
the author fixture registers a unique user through the API and assigns Author
through psql in this project's Docker Compose postgres service. The default
Student role is retained. Docker Compose and the running local stack are required;
the API target must use that same database. Setup errors fail the test.

If your .env predates this setting, add TEST_PROVISION_AUTHOR=1 once.
Both local runs and CI load .env through uv; pytest does not load it itself.

For another backend, supply TEST_AUTHOR_EMAIL and TEST_AUTHOR_PASSWORD for an
existing Author account. These take precedence over automatic provisioning.
Both values must be provided together. Do not commit credentials.
The fixture obtains the account ID and JWT through login and profile requests.

Run from tests/:

```bash
uv run --env-file ../.env pytest -v api/test_author_courses.py --junitxml=author-courses.xml
```

Generated authors and courses remain in the test database after execution.
Courses use unique titles.
The creation test expects a JSON array from GET /api/author/courses containing
full course objects without pagination, including Draft courses.
