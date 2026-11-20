import os
from uuid import uuid4

import httpx
import pytest


def pytest_addoption(parser):
    parser.addoption(
        "--run-jwt-signing",
        action="store_true",
        help="Include tests that require the test backend JWT_KEY.",
    )


@pytest.hookimpl(trylast=True)
def pytest_collection_modifyitems(config, items):
    if config.getoption("--run-jwt-signing"):
        return

    selected = []
    deselected = []
    for item in items:
        if item.get_closest_marker("jwt_signing") is not None:
            deselected.append(item)
        else:
            selected.append(item)

    items[:] = selected
    if deselected:
        config.hook.pytest_deselected(items=deselected)


@pytest.fixture
def api_client():
    base_url = os.getenv("API_BASE_URL", "http://localhost:8080")

    with httpx.Client(base_url=base_url, timeout=10.0) as client:
        yield client


@pytest.fixture
def user_data():
    return {
        "email": f"test-{uuid4()}@example.com",
        "password": "Password123!",
        "firstName": "Ivan",
        "lastName": "Ivanov",
    }


@pytest.fixture
def registered_user(api_client, user_data):
    response = api_client.post("/api/auth/register", json=user_data)
    assert response.status_code == 201, response.text

    return {**user_data, "id": response.json()["id"]}


@pytest.fixture
def access_token(api_client, registered_user):
    response = api_client.post(
        "/api/auth/login",
        json={
            "email": registered_user["email"],
            "password": registered_user["password"],
        },
    )
    assert response.status_code == 200, response.text

    return response.json()["accessToken"]
