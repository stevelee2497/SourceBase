# CLAUDE.md — SourceBase.Wallets

Standalone wallet module (React + Cloudflare Workers + D1 + R2 + Auth0). Independent of the .NET projects; it will move to its own repository.

Load `/jupiter-wallet` (`.claude/skills/jupiter-wallet/SKILL.md`) before writing code here. The one rule above all others: keep it simple.

- Backend: `backend/` — `npm --prefix /abs/path/SourceBase.Wallets/backend test | run typecheck | run dev`.
- Frontend: `frontend/` — `npm --prefix /abs/path/SourceBase.Wallets/frontend test | run build | run dev`.
- Docs: `docs/api.md` (contract), `docs/setup-and-deploy.md` (setup, Auth0, D1, R2, Pages), `docs/requirements.md` (original brief).
- Do not touch other `SourceBase.*` projects from here.
