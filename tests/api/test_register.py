from uuid import UUID

import pytest


def test_register_success(api_client, user_data):
    response = api_client.post("/api/auth/register", json=user_data)

    assert response.status_code == 201, response.text

    data = response.json()
    assert UUID(data["id"]).int != 0
    assert data["message"] == "Account created. You can sign in."

    login = api_client.post(
        "/api/auth/login",
        json={"email": user_data["email"], "password": user_data["password"]},
    )
    assert login.status_code == 200, login.text
    token = login.json()
    assert token["accessToken"]
    assert token["tokenType"] == "Bearer"

    profile = api_client.get(
        "/api/auth/me",
        headers={"Authorization": f"Bearer {token['accessToken']}"},
    )
    assert profile.status_code == 200, profile.text
    user = profile.json()
    assert user["id"] == data["id"]
    assert user["email"] == user_data["email"]
    assert user["firstName"] == user_data["firstName"]
    assert user["lastName"] == user_data["lastName"]
    assert user["roles"] == ["Student"]


def test_register_duplicate_email(api_client, user_data):
    created = api_client.post("/api/auth/register", json=user_data)
    assert created.status_code == 201, created.text

    response = api_client.post("/api/auth/register", json=user_data)
    assert response.status_code == 409, response.text
    assert response.json()["message"] == "An account with this email already exists."


@pytest.mark.parametrize("field", ["email", "password", "firstName", "lastName"])
@pytest.mark.parametrize("value", [None, "", "   "])
def test_register_required_fields(api_client, user_data, field, value):
    user_data[field] = value

    response = api_client.post("/api/auth/register", json=user_data)
    assert response.status_code == 400, response.text
    errors = response.json()["errors"]
    assert errors[field[0].upper() + field[1:]]


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
def test_register_invalid_credentials(api_client, user_data, field, value):
    user_data[field] = value

    response = api_client.post("/api/auth/register", json=user_data)
    assert response.status_code == 400, response.text
    assert response.json()["errors"]

    # A rejected request must not leave an account with the submitted email.
    if field == "password":
        user_data["password"] = "Password123!"
        corrected = api_client.post("/api/auth/register", json=user_data)
        assert corrected.status_code == 201, corrected.text


@pytest.mark.parametrize("field", ["email", "password", "firstName", "lastName"])
def test_register_missing_required_field(api_client, user_data, field):
    valid_user_data = user_data.copy()
    user_data.pop(field)

    response = api_client.post("/api/auth/register", json=user_data)

    assert response.status_code == 400, response.text
    errors = response.json()["errors"]
    assert errors[field[0].upper() + field[1:]]

    corrected = api_client.post("/api/auth/register", json=valid_user_data)
    assert corrected.status_code == 201, corrected.text


@pytest.mark.parametrize(
    ("field", "value"),
    [
        ("email", f"{'a' * 64}@{'b' * 63}.{'c' * 63}.{'d' * 60}.com"),
        ("firstName", "A" * 101),
        ("lastName", "A" * 101),
    ],
)
def test_register_exceeds_max_length(api_client, user_data, field, value):
    user_data[field] = value

    response = api_client.post("/api/auth/register", json=user_data)

    assert response.status_code == 400, response.text
    errors = response.json()["errors"]
    assert errors[field[0].upper() + field[1:]]


@pytest.mark.parametrize(
    ("field", "value"),
    [
        ("firstName", "И" * 100),
        ("lastName", "Я" * 100),
    ],
)
def test_register_name_at_max_length(api_client, user_data, field, value):
    user_data[field] = value

    response = api_client.post("/api/auth/register", json=user_data)
    assert response.status_code == 201, response.text

    login = api_client.post(
        "/api/auth/login",
        json={"email": user_data["email"], "password": user_data["password"]},
    )
    assert login.status_code == 200, login.text

    profile = api_client.get(
        "/api/auth/me",
        headers={"Authorization": f"Bearer {login.json()['accessToken']}"},
    )
    assert profile.status_code == 200, profile.text
    user = profile.json()
    assert user["id"] == response.json()["id"]
    assert user["firstName"] == user_data["firstName"]
    assert user["lastName"] == user_data["lastName"]


@pytest.mark.parametrize(
    ("password", "expected_status"),
    [
        ("Aa1!aaa", 400),
        ("Aa1!aaaa", 201),
    ],
)
def test_register_password_min_length(
    api_client, user_data, password, expected_status
):
    user_data["password"] = password

    response = api_client.post("/api/auth/register", json=user_data)

    assert response.status_code == expected_status, response.text
    if expected_status == 400:
        assert response.json()["errors"]["Password"]
        user_data["password"] = "Aa1!aaaa"
        corrected = api_client.post("/api/auth/register", json=user_data)
        assert corrected.status_code == 201, corrected.text
    else:
        assert UUID(response.json()["id"]).int != 0


def test_register_duplicate_email_case_insensitive(api_client, user_data):
    created = api_client.post("/api/auth/register", json=user_data)
    assert created.status_code == 201, created.text

    user_data["email"] = user_data["email"].upper()
    response = api_client.post("/api/auth/register", json=user_data)

    assert response.status_code == 409, response.text
    assert response.json()["message"] == "An account with this email already exists."
