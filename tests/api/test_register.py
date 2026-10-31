import os
from uuid import UUID, uuid4

import httpx
import pytest


@pytest.fixture
def client():
    base_url = os.getenv("API_BASE_URL", "http://localhost:8080").rstrip("/")
    with httpx.Client(base_url=base_url, timeout=10) as client:
        yield client


@pytest.fixture
def registration():
    return {
        "email": f"test-{uuid4()}@example.com",
        "password": "Password123!",
        "firstName": "Ivan",
        "lastName": "Ivanov",
    }


def test_register_success(client, registration):
    response = client.post("/api/auth/register", json=registration)

    assert response.status_code == 201, response.text

    data = response.json()
    assert UUID(data["id"]).int != 0
    assert data["message"] == "Account created. You can sign in."

    login = client.post(
        "/api/auth/login",
        json={"email": registration["email"], "password": registration["password"]},
    )
    assert login.status_code == 200, login.text
    token = login.json()
    assert token["accessToken"]
    assert token["tokenType"] == "Bearer"

    profile = client.get(
        "/api/auth/me",
        headers={"Authorization": f"Bearer {token['accessToken']}"},
    )
    assert profile.status_code == 200, profile.text
    user = profile.json()
    assert user["id"] == data["id"]
    assert user["email"] == registration["email"]
    assert user["firstName"] == registration["firstName"]
    assert user["lastName"] == registration["lastName"]
    assert user["roles"] == ["Student"]


def test_register_duplicate_email(client, registration):
    created = client.post("/api/auth/register", json=registration)
    assert created.status_code == 201, created.text

    response = client.post("/api/auth/register", json=registration)
    assert response.status_code == 409, response.text
    assert response.json()["message"] == "An account with this email already exists."


@pytest.mark.parametrize("field", ["email", "password", "firstName", "lastName"])
@pytest.mark.parametrize("value", [None, "", "   "])
def test_register_required_fields(client, registration, field, value):
    registration[field] = value

    response = client.post("/api/auth/register", json=registration)
    assert response.status_code == 400, response.text
    assert response.json()["errors"]


@pytest.mark.parametrize(
    ("field", "value"),
    [
        ("email", "invalid-email"),
        ("password", "Aa1!"),
        ("password", "password123!"),
        ("password", "PASSWORD123!"),
        ("password", "Password!!!"),
        ("password", "Password123"),
    ],
)
def test_register_invalid_credentials(client, registration, field, value):
    registration[field] = value

    response = client.post("/api/auth/register", json=registration)
    assert response.status_code == 400, response.text
    assert response.json()["errors"]

    # A rejected request must not leave an account with the submitted email.
    if field == "password":
        registration["password"] = "Password123!"
        corrected = client.post("/api/auth/register", json=registration)
        assert corrected.status_code == 201, corrected.text
