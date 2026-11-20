from datetime import datetime, timedelta, timezone
from re import fullmatch

import pytest


def test_login_success(api_client, registered_user):
    response = api_client.post(
        "/api/auth/login",
        json={
            "email": registered_user["email"],
            "password": registered_user["password"],
        },
    )

    assert response.status_code == 200, response.text
    data = response.json()
    assert isinstance(data, dict)
    assert isinstance(data["accessToken"], str)
    assert fullmatch(
        r"[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+",
        data["accessToken"],
    )
    assert data["tokenType"] == "Bearer"
    assert isinstance(data["expiresAtUtc"], str)
    expires_at = datetime.fromisoformat(data["expiresAtUtc"])
    assert expires_at.utcoffset() == timedelta(0)
    assert expires_at > datetime.now(timezone.utc)


def test_login_wrong_password(api_client, registered_user):
    response = api_client.post(
        "/api/auth/login",
        json={
            "email": registered_user["email"],
            "password": "WrongPassword123!",
        },
    )

    assert response.status_code == 401, response.text
    data = response.json()
    assert data["message"] == "Invalid email or password."
    assert "accessToken" not in data


def test_login_unknown_email(api_client, user_data):
    response = api_client.post(
        "/api/auth/login",
        json={"email": user_data["email"], "password": user_data["password"]},
    )

    assert response.status_code == 401, response.text
    data = response.json()
    assert data["message"] == "Invalid email or password."
    assert "accessToken" not in data


@pytest.mark.parametrize("field", ["email", "password"])
@pytest.mark.parametrize("value", [None, "", "   "])
def test_login_required_fields(api_client, user_data, field, value):
    credentials = {
        "email": user_data["email"],
        "password": user_data["password"],
    }
    credentials[field] = value

    response = api_client.post("/api/auth/login", json=credentials)

    assert response.status_code == 400, response.text
    data = response.json()
    assert data["errors"][field[0].upper() + field[1:]]
    assert "accessToken" not in data


@pytest.mark.parametrize("field", ["email", "password"])
def test_login_missing_required_field(api_client, user_data, field):
    credentials = {
        "email": user_data["email"],
        "password": user_data["password"],
    }
    credentials.pop(field)

    response = api_client.post("/api/auth/login", json=credentials)

    assert response.status_code == 400, response.text
    data = response.json()
    assert data["errors"][field[0].upper() + field[1:]]
    assert "accessToken" not in data


def test_login_invalid_email(api_client, user_data):
    response = api_client.post(
        "/api/auth/login",
        json={"email": "invalid-email", "password": user_data["password"]},
    )

    assert response.status_code == 400, response.text
    data = response.json()
    assert data["errors"]["Email"]
    assert "accessToken" not in data
