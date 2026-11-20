import os
from uuid import UUID, uuid4

import pytest


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
    if not email or not password:
        pytest.fail(
            "Set TEST_AUTHOR_EMAIL and TEST_AUTHOR_PASSWORD "
            "for an Author account on the test backend."
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
