from concurrent.futures import ThreadPoolExecutor
from threading import Barrier
from uuid import UUID

import pytest


pytestmark = pytest.mark.perfomance


def test_register_same_email_concurrently(api_client, user_data):
    payloads = [
        {
            **user_data,
            "firstName": f"Concurrent{index}",
            "password": f"Password123!{index}",
        }
        for index in range(4)
    ]
    barrier = Barrier(len(payloads), timeout=10)

    def register(payload):
        barrier.wait()
        return api_client.post("/api/auth/register", json=payload)

    with ThreadPoolExecutor(max_workers=len(payloads)) as executor:
        futures = [executor.submit(register, payload) for payload in payloads]
        responses = [future.result(timeout=20) for future in futures]

    statuses = [response.status_code for response in responses]
    details = [(response.status_code, response.text) for response in responses]
    assert sorted(statuses) == [201, 409, 409, 409], details

    winner_index = statuses.index(201)
    winner = payloads[winner_index]
    created = responses[winner_index].json()
    assert UUID(created["id"]).int != 0
    assert created["message"] == "Account created. You can sign in."

    for response in responses:
        if response.status_code == 409:
            assert response.json()["message"] == (
                "An account with this email already exists."
            )

    login = api_client.post(
        "/api/auth/login",
        json={"email": winner["email"], "password": winner["password"]},
    )
    assert login.status_code == 200, login.text

    profile = api_client.get(
        "/api/auth/me",
        headers={"Authorization": f"Bearer {login.json()['accessToken']}"},
    )
    assert profile.status_code == 200, profile.text
    assert profile.json() == {
        "id": created["id"],
        "email": winner["email"],
        "firstName": winner["firstName"],
        "lastName": winner["lastName"],
        "roles": ["Student"],
    }

    for index, payload in enumerate(payloads):
        if index == winner_index:
            continue
        rejected_login = api_client.post(
            "/api/auth/login",
            json={"email": payload["email"], "password": payload["password"]},
        )
        assert rejected_login.status_code == 401, rejected_login.text
        data = rejected_login.json()
        assert data["message"] == "Invalid email or password."
        assert "accessToken" not in data
