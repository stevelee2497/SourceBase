# Deployment

## Current deployment model

SourceBase now has two API deployment paths:

1. **Legacy .NET API on VPS** via `api-publish.yml`
2. **Cloudflare Workers API** via `workers-publish.yml`

The web app deploy (`web-publish.yml`) already supports the Workers cutover through `WORKERS_API_URL`.

---

## GitHub Production environment

Go to **GitHub → repo → Settings → Environments → Production**.

### Variables

| Variable name       | Description |
| ------------------- | ----------- |
| `ADMIN_EMAIL`       | Seeded admin email for the legacy/API bootstrap paths |
| `POSTGRES_USER`     | PostgreSQL username for the VPS deployment |
| `POSTGRES_DB`       | PostgreSQL database name for the VPS deployment |
| `WEB_URL`           | Web app URL, such as `https://app.example.com` |
| `API_URL`           | Legacy .NET API URL, such as `https://api.example.com` |
| `WORKERS_API_URL`   | Cloudflare Workers API URL used by the web app, such as `https://sourcebase-workers.example.workers.dev` |
| `CF_PROJECT_NAME`   | Cloudflare Pages project name for the web frontend |
| `CF_URL`            | R2 endpoint used by the VPS backup flow |
| `CF_PUBLIC_URL`     | Public R2 asset/base URL used by the legacy stack |

### Secrets

| Secret name              | Description |
| ------------------------ | ----------- |
| `CF_API_TOKEN`           | Cloudflare API token used by GitHub Actions to deploy Workers and Pages |
| `CF_ACCOUNT_ID`          | Cloudflare account ID |
| `SENDGRID_API_KEY`       | SendGrid API key |
| `SENDGRID_ACCOUNT_OWNER` | SendGrid account owner email |
| `POSTGRES_PASSWORD`      | PostgreSQL password for VPS deploys |
| `ADMIN_PASSWORD`         | Seeded admin password |
| `REDIS_PASSWORD`         | Redis password for the legacy stack |
| `BETTERSTACK_SOURCE_TOKEN` | Better Stack log ingestion token |
| `CF_ACCESS_KEY_ID`       | R2 access key ID for VPS backup/upload flows |
| `CF_SECRET_ACCESS_KEY`   | R2 secret access key for VPS backup/upload flows |
| `VPS_HOST`               | VPS hostname or IP |
| `VPS_USER`               | VPS SSH user |
| `VPS_SSH_KEY`            | VPS SSH private key |

---

## Cloudflare Workers API setup

### 1. Create Cloudflare resources

Create these resources in Cloudflare:

- a **Workers** service for `sourcebase-workers`
- a **D1** database named `sourcebase-workers`
- an **R2** bucket if you want avatar upload signing enabled
- an optional **custom domain** for the Workers API, such as `api.example.com`
- a **Cloudflare API token** for GitHub Actions with permissions to deploy Workers, manage D1 migrations, and deploy Pages

### 2. Update Wrangler config

Open `SourceBase.Workers/wrangler.jsonc` and replace the placeholder D1 ID:

```jsonc
"database_id": "replace-me"
```

with the real Cloudflare D1 database ID.

### 3. Configure Worker runtime values in Cloudflare

Set these directly on the Worker in the Cloudflare dashboard.

#### Worker secrets

- `JWT_SECRET`
- `REFRESH_TOKEN_SECRET`
- `ADMIN_PASSWORD`
- `R2_SECRET_ACCESS_KEY` when avatar upload signing is enabled

#### Worker vars

- `ADMIN_EMAIL`
- `ACCESS_TOKEN_EXPIRES_IN`
- `REFRESH_TOKEN_EXPIRES_IN`
- `OTP_TOKEN_EXPIRATION_MINUTES`
- `WEB_URL`
- `ALLOWED_ORIGINS`
- `REDIS_AVAILABLE`
- `R2_ACCESS_KEY_ID`
- `R2_BUCKET_NAME`
- `R2_ACCOUNT_ID`
- `R2_PUBLIC_URL`
- `R2_UPLOAD_EXPIRES_IN`

Recommended values:

- `WEB_URL=https://app.example.com`
- `ALLOWED_ORIGINS=https://app.example.com`
- `REDIS_AVAILABLE=false`

If you serve the web app from more than one origin, set `ALLOWED_ORIGINS` as a comma-separated list.

### 4. Deploy through GitHub Actions

The new workflow `.github/workflows/workers-publish.yml`:

1. installs Node dependencies in `SourceBase.Workers`
2. runs `npm run typecheck`
3. runs `npm test`
4. applies remote D1 migrations
5. deploys the Worker with Wrangler

It uses:

- `CF_API_TOKEN`
- `CF_ACCOUNT_ID`

If `wrangler.jsonc` still contains `database_id: "replace-me"`, the workflow fails fast before deployment.

### 5. Cut the web app over to Workers

Once the Worker is deployed and reachable:

1. set `WORKERS_API_URL` in the GitHub Production environment to the deployed Workers URL or custom domain
2. keep `API_URL` unchanged for rollback safety
3. redeploy the web app through `web-publish.yml`

The web app already prefers `WORKERS_API_URL` over `API_URL`.

### 6. Migrate production data

The Workers API can bootstrap schema and seed an admin locally, but that is **not** a full production migration.

You still need a one-time **PostgreSQL → D1** migration for real data:

- users and roles
- wallets, categories, transactions, transfers
- todo lists and todos
- time sheets
- habits and habit logs
- notifications
- icons
- machines
- gold prices
- audits

Keep the legacy `.NET` API online until the imported D1 data and production Worker are smoke-tested.

---

## Legacy VPS API deployment

Use this while the .NET API remains your fallback or rollback path.

### First-time setup

```sh
git clone https://github.com/stevelee2497/SourceBase.git ~/SourceBase
cd ~/SourceBase
docker compose up -d
```

Caddy automatically obtains and renews TLS certificates from Let's Encrypt when the containers start.

### Legacy API deploy updates

Pushes to `main` automatically build and deploy via `.github/workflows/api-publish.yml`.

To deploy manually:

```sh
cd ~/SourceBase
git pull
docker compose pull
docker compose up -d --remove-orphans
```
