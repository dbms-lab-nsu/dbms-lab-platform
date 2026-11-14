from uuid import UUID, uuid4


def test_me_success(api_client, registered_user, access_token):
    response = api_client.get(
        "/api/auth/me",
        headers={"Authorization": f"Bearer {access_token}"},
    )

    assert response.status_code == 200, response.text
    user = response.json()
    assert isinstance(user, dict)
    assert isinstance(user["id"], str)
    assert UUID(user["id"]).int != 0
    assert user == {
        "id": registered_user["id"],
        "email": registered_user["email"],
        "firstName": registered_user["firstName"],
        "lastName": registered_user["lastName"],
        "roles": ["Student"],
    }


def test_me_without_token(api_client):
    response = api_client.get("/api/auth/me")

    assert response.status_code == 401, response.text


def test_me_malformed_token(api_client):
    response = api_client.get(
        "/api/auth/me",
        headers={"Authorization": "Bearer not-a-jwt"},
    )

    assert response.status_code == 401, response.text


def test_me_modified_signature(api_client, access_token):
    valid = api_client.get(
        "/api/auth/me",
        headers={"Authorization": f"Bearer {access_token}"},
    )
    assert valid.status_code == 200, valid.text

    header, payload, signature = access_token.split(".")
    replacement = "A" if signature[0] != "A" else "B"
    modified_token = f"{header}.{payload}.{replacement}{signature[1:]}"

    response = api_client.get(
        "/api/auth/me",
        headers={"Authorization": f"Bearer {modified_token}"},
    )

    assert response.status_code == 401, response.text


def test_me_returns_token_owner(api_client, registered_user, access_token):
    second_user = {
        "email": f"test-{uuid4()}@example.com",
        "password": registered_user["password"],
        "firstName": "Anna",
        "lastName": "Petrova",
    }
    created = api_client.post("/api/auth/register", json=second_user)
    assert created.status_code == 201, created.text
    second_user["id"] = created.json()["id"]
    assert second_user["id"] != registered_user["id"]

    login = api_client.post(
        "/api/auth/login",
        json={
            "email": second_user["email"],
            "password": second_user["password"],
        },
    )
    assert login.status_code == 200, login.text
    second_token = login.json()["accessToken"]

    for token, expected_user in [
        (access_token, registered_user),
        (second_token, second_user),
    ]:
        response = api_client.get(
            "/api/auth/me",
            headers={"Authorization": f"Bearer {token}"},
        )
        assert response.status_code == 200, response.text
        assert response.json() == {
            "id": expected_user["id"],
            "email": expected_user["email"],
            "firstName": expected_user["firstName"],
            "lastName": expected_user["lastName"],
            "roles": ["Student"],
        }
