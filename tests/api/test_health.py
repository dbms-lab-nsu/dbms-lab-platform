import os

import httpx


def test_backend_health():
    base_url = os.getenv("API_BASE_URL", "http://localhost:8080")

    response = httpx.get(f"{base_url}/api/health")

    assert response.status_code == 200
    assert response.json()["status"] == "OK"


    