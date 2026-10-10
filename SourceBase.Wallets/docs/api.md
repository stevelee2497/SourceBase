# Jupiter Wallet API

Base URL: `{API_URL}/api`. Every route needs `Authorization: Bearer <Auth0 access token>` except `GET /api/icons/files/:key`.

- JSON is camelCase. Null fields are omitted from responses.
- Dates are `yyyy-MM-dd`. Enums are strings: `Income` / `Expense`, `Wallet` / `Category` / `General`.
- Incoming strings are trimmed.
- User identity is the Auth0 `sub` claim. Every row is scoped to it.

## Errors

```json
{ "traceId": "…", "code": "VALIDATION ERROR", "message": "One or more validation errors occurred.", "errors": { "name": ["Name is required."] } }
```

| Status | `code` |
|---|---|
| 400 | `VALIDATION ERROR` (field errors in `errors`) or `BAD REQUEST` |
| 401 | `UNAUTHORIZE` |
| 403 | `FORBIDDEN` |
| 404 | `NOT FOUND` |

## Wallets

| Method | Route | Body / query | Response |
|---|---|---|---|
| GET | `/wallets` | — | `{ wallets: Wallet[], totalBalance }` |
| GET | `/wallets/:id` | — | `Wallet` |
| POST | `/wallets` | `{ name*, initialBalance, currency*, icon? }` | `{ id }` |
| PATCH | `/wallets/:id` | `{ name?, icon? }` | `{ id }` |
| PUT | `/wallets/:id/config` | `{ currency* }` (stored upper-case, ≤10) | `{ id }` |
| DELETE | `/wallets/:id` | — | `{ success: true }` |

`Wallet = { id, name, balance, initialBalance, currency, icon? }`.

- `balance = initialBalance + Σ income − Σ expense`. Transfer legs count toward it.
- `name` is at most 100 characters.
- PATCH keeps the old value for any field that is omitted.

## Transactions

| Method | Route | Body / query | Response |
|---|---|---|---|
| GET | `/transactions` | `walletIds` (repeat), `type`, `categoryIds` (repeat), `dateFrom`, `dateTo`, `exclude=Transfer`, `page=1`, `limit=10`, `order=Desc`, `orderBy=Date\|Amount\|Type` | `{ items: Transaction[], page, limit, total }` |
| GET | `/transactions/:id` | — | `Transaction` |
| GET | `/transactions/summary` | `walletIds` (repeat), `dateFrom`, `dateTo` | `{ totalIncome, totalExpense, netBalance, byCategory: [{ categoryId?, categoryName?, type, total }] }` |
| POST | `/transactions` | `{ walletId*, amount*, type*, date*, note?, categoryId* }` | `{ id }` |
| PATCH | `/transactions/:id` | `{ walletId?, amount?, type?, date?, note?, categoryId? }` | `{ id }` |
| DELETE | `/transactions/:id` | — | `{ success: true }` |

`Transaction = { id, amount, type, date, note?, walletId, walletName, categoryId?, categoryName?, isTransfer }`.

- `amount` must be greater than 0.
- `walletId` must belong to you. `categoryId` must be a system category or one of yours. Otherwise the request fails with 400.
- Transfer legs can't be edited or deleted here; doing so returns 400. Use `/transfers` instead.
- The summary leaves transfers out.

## Transfers

| Method | Route | Body / query | Response |
|---|---|---|---|
| GET | `/transfers` | `walletId` (matches from or to), `dateFrom`, `dateTo`, `page`, `limit`, `order`, `orderBy=Date\|Amount` | `{ items: Transfer[], page, limit, total }` |
| POST | `/transfers` | `{ fromWalletId*, toWalletId*, amount*, date*, note? }` | `{ id }` |
| DELETE | `/transfers/:id` | — | `{ success: true }` |

`Transfer = { id, fromWalletId, fromWalletName, toWalletId, toWalletName, amount, date, note? }`.

A transfer writes an Expense leg on the source wallet and an Income leg on the destination wallet, both with `isTransfer=true`. Both legs and the transfer row are written in one atomic D1 batch.

## Categories

| Method | Route | Body / query | Response |
|---|---|---|---|
| GET | `/categories` | `type?` | `Category[]` (system + yours, ordered by name) |
| POST | `/categories` | `{ name*, type*, icon? }` | `{ id }` |
| PATCH | `/categories/:id` | `{ name?, icon? }` | `{ id }` |
| DELETE | `/categories/:id` | — | `{ success: true }` |

`Category = { id, userId?, name, type, icon?, isSystem }`.

- Updating or deleting a system category returns 403.
- Deleting a category that transactions use returns 400.

## Icons

| Method | Route | Body / query | Response |
|---|---|---|---|
| GET | `/icons` | `group?` (also includes `General`) | `Icon[]` (ordered by sortOrder) |
| POST | `/icons` | `{ value*, name*, group*, sortOrder }` | `{ id }` |
| PATCH | `/icons/:id` | `{ value?, name?, group?, sortOrder? }` | `{ id }` |
| DELETE | `/icons/:id` | — | `{ success: true }` |
| POST | `/icons/upload` | multipart `file` (jpg/png/gif/webp/svg, ≤5 MB) | `{ iconUrl }` |
| GET | `/icons/files/:key` | public, no auth | the image bytes |

`Icon = { id, value, name, group, sortOrder, isSystem }`.

- `value` is an emoji, an SVG `<path …/>` fragment (24×24), or an image URL.
- Updating or deleting a system icon returns 403.

## Differences from the .NET API

1. `GET /transactions/summary` takes repeated `walletIds`, not a single `walletId`, so the Monthly Income/Expense tiles work with several wallets selected.
2. Deleting a wallet also deletes every transfer that touches it, together with **both** legs. In .NET the other wallet's leg was left behind as an orphan that couldn't be deleted.
3. User-created icons belong to their creator. In .NET they were global and anyone could edit them. System icons are still shared.
4. Creating a transfer is atomic (one D1 batch).
5. Icon upload is a multipart POST to the Worker, which stores the file in R2 and serves it. It replaces the presigned R2 URL flow.
6. User ids are Auth0 `sub` strings, and ids are UUID strings.
