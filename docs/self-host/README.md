# Self-Hosting Aptabase

Short guide to self-host **Aptabase** with Docker + Caddy + HTTPS.

This is meant to be simple and fire-and-forget.

---

## 1. Server

Rent a small VPS.  
Example that works well and is cheap:

OVHcloud VPS-1  
4 vCPU, **8 GB RAM**, 75 GB SSD (~$4.20/month)

Important: Aptabase uses **ClickHouse** for events.  
1–2 GB RAM will not work reliably.  
8 GB RAM is the safe minimum and still cheap.

---

## 2. Install Docker

Install Docker on Ubuntu:
https://docs.docker.com/engine/install/ubuntu/

Check:

```console
docker --version
docker compose version
```

---

## 3. Install uv (used for helper script)

https://docs.astral.sh/uv/getting-started/installation/

```console
uv --version
```

---

## 4. Domain + HTTPS (required)

Buy a domain (Cloudflare is recommended, ~$5–10/year):
https://www.cloudflare.com/

This is important because Aptabase auth cookies use `Secure=true`  
and require HTTPS.

Create DNS record:

- analytics.yourdomain.com → server IP
- Proxy ON (orange cloud)

---

## 5. Environment (.env)

Copy the example env file and edit it:

```console
cp .env.example .env
```

Generate a strong random secret:

```console
openssl rand -base64 48
```

Then set your values in the `.env` file.

---

## 6. Docker Compose

Use Docker Compose with:

- Caddy (HTTPS + reverse proxy)
- Postgres (users)
- ClickHouse (events)
- Aptabase app

Bring everything up:

```console
mkdir -p data/postgres data/clickhouse
docker compose up -d
```

Caddy will automatically issue HTTPS certificates.

---

## 7. Verify

Open in browser:
https://analytics.yourdomain.com

If it loads, you are done.

---

## 8. Create one-time login link (optional)

On the server, use this helper script to generate a magic login URL:

```console
uv run create_auth_link.py \
 --name YourName \
 --email you@example.com
```

The printed link is single-use and short-lived.

## 9. Send a test event

To verify everything works end-to-end, send a test event.

Make sure to set the `BASE_URL` and `APTABASE_APP_KEY` in the `.env` file.

```console
uv run create_test_event.py
```

---

That’s it.
This setup is stable, cheap, and production-ready.
