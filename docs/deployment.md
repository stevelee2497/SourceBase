# Deployment

## Prerequisites

- A VPS running Docker and Docker Compose
- DNS A records pointing to the VPS IP
- Ports 80 and 443 open in the firewall

---

## First-time setup

### 1. Add values to the Production environment

Go to **GitHub → repo → Settings → Environments → Production**.

**Environment Variables** (visible in UI):

| Variable name       | Description                          |
| ------------------- | ------------------------------------ |
| `ADMIN_EMAIL`       | Seeded admin account email           |
| `ADMIN_PASSWORD`    | Seeded admin account password        |
| `POSTGRES_USER`     | PostgreSQL username                  |
| `POSTGRES_PASSWORD` | PostgreSQL password                  |
| `POSTGRES_DB`       | PostgreSQL database name             |
| `WEB_URL`           | Web app URL (e.g. `app.example.com`) |
| `API_URL`           | API URL (e.g. `api.example.com`)     |

**Environment Secrets** (masked — keep these as secrets):

| Secret name              | Description                  |
| ------------------------ | ---------------------------- |
| `SENDGRID_API_KEY`       | SendGrid API key             |
| `SENDGRID_ACCOUNT_OWNER` | SendGrid account owner email |

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
