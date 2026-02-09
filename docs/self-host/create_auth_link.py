#!/usr/bin/env -S uv run
# /// script
# requires-python = ">=3.12"
# dependencies = [
#     "pyjwt==2.11.0",
#     "python-dotenv==1.0.1",
# ]
# ///

"""
Usage:
uv run auth_link.py --name YourName --email you@example.com
"""

import argparse
import os
import time
from urllib.parse import quote

import jwt
from dotenv import load_dotenv

load_dotenv()


def require_env(key: str) -> str:
    value = os.environ.get(key)
    if not value:
        raise SystemExit(f"Missing {key} in .env")
    return value


def build_issuer(region: str) -> str:
    return f"aptabase-{region.strip().lower()}"


def build_token(
    auth_secret: str,
    issuer: str,
    name: str,
    email: str,
    expires_in_minutes: int,
) -> str:
    now = int(time.time())
    payload = {
        "type": "Register",
        "name": name,
        "email": email,
        "exp": now + (expires_in_minutes * 60),
        "iat": now,
        "iss": issuer,
    }
    return jwt.encode(payload, auth_secret, algorithm="HS256")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--name", required=True)
    parser.add_argument("--email", required=True)
    parser.add_argument("--expires-in-minutes", type=int, default=10)
    args = parser.parse_args()

    if args.expires_in_minutes <= 0 or args.expires_in_minutes > 15:
        raise SystemExit("--expires-in-minutes must be between 1 and 15")

    base_url = require_env("BASE_URL")
    auth_secret = require_env("AUTH_SECRET")
    region = os.environ.get("APTABASE_REGION", "SH")

    issuer = build_issuer(region)
    token = build_token(
        auth_secret=auth_secret,
        issuer=issuer,
        name=args.name,
        email=args.email,
        expires_in_minutes=args.expires_in_minutes,
    )

    url = f"{base_url.rstrip('/')}/api/_auth/continue?token={quote(token, safe='')}"
    print(url)


if __name__ == "__main__":
    main()
