import base64
import hashlib
import hmac
import json
import os
import secrets

import pytest


pytestmark = pytest.mark.jwt


def _encode_json(value):
    encoded = json.dumps(value, separators=(",", ":")).encode("utf-8")
    return base64.urlsafe_b64encode(encoded).rstrip(b"=").decode("ascii")


@pytest.fixture
def jwt_parts(api_client, access_token):
    response = api_client.get(
        "/api/auth/me",
        headers={"Authorization": f"Bearer {access_token}"},
    )
    assert response.status_code == 200, response.text

    return access_token.split(".")


@pytest.mark.parametrize(
    ("claim", "value"),
    [
        ("exp", 4102444800),
        ("role", "Administrator"),
    ],
)
def test_jwt_modified_payload(api_client, jwt_parts, claim, value):
    header, payload, signature = jwt_parts
    padding = "=" * (-len(payload) % 4)
    claims = json.loads(base64.urlsafe_b64decode(payload + padding))
    assert claims.get(claim) != value
    claims[claim] = value
    modified_token = f"{header}.{_encode_json(claims)}.{signature}"

    response = api_client.get(
        "/api/auth/me",
        headers={"Authorization": f"Bearer {modified_token}"},
    )

    assert response.status_code == 401, response.text


def test_jwt_unsigned(api_client, jwt_parts):
    _, payload, _ = jwt_parts
    header = _encode_json({"alg": "none", "typ": "JWT"})
    unsigned_token = f"{header}.{payload}."

    response = api_client.get(
        "/api/auth/me",
        headers={"Authorization": f"Bearer {unsigned_token}"},
    )

    assert response.status_code == 401, response.text


def test_jwt_signed_with_wrong_key(api_client, jwt_parts):
    header, payload, _ = jwt_parts
    signing_input = f"{header}.{payload}".encode("ascii")
    signature = hmac.new(
        secrets.token_bytes(32), signing_input, hashlib.sha256
    ).digest()
    encoded_signature = (
        base64.urlsafe_b64encode(signature).rstrip(b"=").decode("ascii")
    )
    forged_token = f"{header}.{payload}.{encoded_signature}"

    response = api_client.get(
        "/api/auth/me",
        headers={"Authorization": f"Bearer {forged_token}"},
    )

    assert response.status_code == 401, response.text


@pytest.fixture
def jwt_signer(jwt_parts):
    key = os.getenv("JWT_KEY")
    if not key:
        pytest.fail("Set JWT_KEY to the signing key of the test backend.")
    signing_key = key.encode("utf-8")
    if len(signing_key) < 32:
        pytest.fail("JWT_KEY must contain at least 32 UTF-8 bytes.")

    header, _, _ = jwt_parts

    def sign(claims):
        payload = _encode_json(claims)
        signing_input = f"{header}.{payload}".encode("ascii")
        signature = hmac.new(
            signing_key, signing_input, hashlib.sha256
        ).digest()
        encoded_signature = (
            base64.urlsafe_b64encode(signature).rstrip(b"=").decode("ascii")
        )
        return f"{header}.{payload}.{encoded_signature}"

    return sign


@pytest.mark.jwt_signing
@pytest.mark.parametrize("claim", ["exp", "nbf", "iss", "aud"])
def test_jwt_invalid_claim(api_client, jwt_parts, jwt_signer, claim):
    _, payload, _ = jwt_parts
    padding = "=" * (-len(payload) % 4)
    claims = json.loads(base64.urlsafe_b64decode(payload + padding))

    valid_token = jwt_signer(claims)
    valid = api_client.get(
        "/api/auth/me",
        headers={"Authorization": f"Bearer {valid_token}"},
    )
    assert valid.status_code == 200, valid.text

    invalid_values = {
        "exp": claims["exp"] - 7200,
        "nbf": claims["exp"] + 3600,
        "iss": f"invalid-{secrets.token_hex(16)}",
        "aud": f"invalid-{secrets.token_hex(16)}",
    }
    claims[claim] = invalid_values[claim]
    invalid_token = jwt_signer(claims)

    response = api_client.get(
        "/api/auth/me",
        headers={"Authorization": f"Bearer {invalid_token}"},
    )

    assert response.status_code == 401, response.text
