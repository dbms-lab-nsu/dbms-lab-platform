import os
import secrets
import subprocess
from pathlib import Path
from uuid import UUID, uuid4

import pytest

pytestmark = pytest.mark.xfail(
    reason=(
        "Pending TASK-19091 integration and Author account provisioning in CI."
    ),
    run=False,
)

@pytest.fixture
def course_data():
    return {
        "title": f"PostgreSQL course {uuid4()}",
        "description": "An introduction to PostgreSQL internals.",
    }


@pytest.fixture
def author(api_client):
    email = os.getenv("TEST_AUTHOR_EMAIL")
    password = os.getenv("TEST_AUTHOR_PASSWORD")
    if bool(email) != bool(password):
        pytest.fail("Set both TEST_AUTHOR_EMAIL and TEST_AUTHOR_PASSWORD.")

    if not email and os.getenv("TEST_PROVISION_AUTHOR") == "1":
        email = f"author-{uuid4()}@example.com"
        password = f"Aa1!{secrets.token_hex(24)}"
        registration = api_client.post(
            "/api/auth/register",
            json={
                "email": email,
                "password": password,
                "firstName": "Test",
                "lastName": "Author",
            },
        )
        assert registration.status_code == 201, registration.text
        user_id = UUID(registration.json()["id"])

        subprocess.run(
            [
                "docker", "compose", "exec", "-T", "postgres", "sh", "-c",
                'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"',
            ],
            cwd=Path(__file__).resolve().parents[2],
            input=(
                'INSERT INTO user_roles ("UserId", "RoleId") '
                f"VALUES ('{user_id}', "
                '(SELECT "Id" FROM roles WHERE "Name" = \'Author\'));\n'
            ),
            text=True,
            check=True,
        )
    elif not email:
        pytest.fail(
            "Load .env with TEST_PROVISION_AUTHOR=1 for the local Compose stack, "
            "or set TEST_AUTHOR_EMAIL and TEST_AUTHOR_PASSWORD "
            "for an existing Author account."
        )

    login = api_client.post(
        "/api/auth/login",
        json={"email": email, "password": password},
    )
    assert login.status_code == 200, login.text
    headers = {"Authorization": f"Bearer {login.json()['accessToken']}"}

    profile = api_client.get("/api/auth/me", headers=headers)
    assert profile.status_code == 200, profile.text
    user = profile.json()
    assert "Author" in user["roles"], "The configured user must have Author role."

    return {"id": user["id"], "headers": headers}


def test_create_course_as_author(api_client, author, course_data):
    response = api_client.post(
        "/api/courses",
        headers=author["headers"],
        json=course_data,
    )

    assert response.status_code == 201, response.text
    course = response.json()
    assert isinstance(course, dict)
    assert isinstance(course["id"], str)
    assert UUID(course["id"]).int != 0
    expected = {
        "id": course["id"],
        "authorId": author["id"],
        "title": course_data["title"],
        "description": course_data["description"],
        "status": "Draft",
    }
    assert course == expected

    listed = api_client.get(
        "/api/author/courses",
        headers=author["headers"],
    )
    assert listed.status_code == 200, listed.text
    courses = listed.json()
    assert isinstance(courses, list)
    assert all(isinstance(item, dict) for item in courses)
    saved = [item for item in courses if item["id"] == course["id"]]
    assert saved == [expected]


def test_create_course_without_token(api_client, course_data):
    response = api_client.post("/api/courses", json=course_data)

    assert response.status_code == 401, response.text


def test_create_course_as_student(api_client, access_token, course_data):
    headers = {"Authorization": f"Bearer {access_token}"}
    profile = api_client.get("/api/auth/me", headers=headers)

    assert profile.status_code == 200, profile.text
    assert profile.json()["roles"] == ["Student"]

    response = api_client.post(
        "/api/courses",
        headers=headers,
        json=course_data,
    )

    assert response.status_code == 403, response.text
