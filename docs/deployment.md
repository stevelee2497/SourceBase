# Deployment

## Prerequisites

- A VPS running Docker and Docker Compose
- DNS A records pointing to the VPS IP:
  - `quoctran.qzz.io` → VPS IP
  - `api.quoctran.qzz.io` → VPS IP
- Ports 80 and 443 open in the firewall

---

## First-time setup

### 1. Clone the repository

```sh
git clone https://github.com/stevelee2497/SourceBase.git ~/SourceBase
cd ~/SourceBase
```

### 2. Create the `.env` file

```sh
cp .env.example .env
# Edit .env with your actual values
```

Key values to set:

| Variable | Value |
|---|---|
| `WEB_URL` | `https://quoctran.qzz.io` |
| `ADMIN_EMAIL` | Your admin email |
| `ADMIN_PASSWORD` | Strong password |
| `POSTGRES_PASSWORD` | Strong password |

### 3. Start all services

```sh
docker compose up -d
```

Caddy automatically obtains and renews TLS certificates from Let's Encrypt when the containers start.
No manual certificate setup is required.

---

## Deploying updates

Pushes to `main` automatically build and deploy via GitHub Actions (see `.github/workflows/docker-publish.yml`).

To deploy manually:

```sh
cd ~/SourceBase
git pull
docker compose pull
docker compose up -d --remove-orphans
```
