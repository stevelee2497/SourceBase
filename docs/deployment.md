# Deployment

## Prerequisites

- A VPS running Docker and Docker Compose
- DNS A records pointing to the VPS IP:
  - `quoctran.qzz.io` → VPS IP
  - `api.quoctran.qzz.io` → VPS IP
- Ports 80 and 443 open in the firewall

---

## First-time setup

### 1. Add values to the Production environment

Go to **GitHub → repo → Settings → Environments → Production**.

**Environment Variables** (visible in UI):

| Variable name            | Description                  |
| ------------------------ | ---------------------------- |
| `POSTGRES_USER`          | PostgreSQL username          |
| `POSTGRES_DB`            | PostgreSQL database name     |
| `ADMIN_EMAIL`            | Seeded admin account email   |
| `SENDGRID_ACCOUNT_OWNER` | SendGrid account owner email |

**Environment Secrets** (masked — keep these as secrets):

| Secret name         | Description                   |
| ------------------- | ----------------------------- |
| `POSTGRES_PASSWORD` | PostgreSQL password           |
| `ADMIN_PASSWORD`    | Seeded admin account password |
| `SENDGRID_API_KEY`  | SendGrid API key              |

### 2. Clone the repository

```sh
git clone https://github.com/stevelee2497/SourceBase.git ~/SourceBase
cd ~/SourceBase
```

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
