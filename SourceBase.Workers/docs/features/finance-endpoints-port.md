# Finance endpoints port: categories, wallets, transactions, transfers

## Goal
Port the current .NET finance slices into `SourceBase.Workers` with route, payload, status, validation, and ownership behavior that stays as close as practical to the existing API.

## Endpoint groups

### Categories
- `GET /api/categories?type=Income|Expense`
  - Returns system categories plus current user's categories.
  - Ordered by `name` ascending.
- `POST /api/categories`
  - Body: `{ name, type, icon? }`
  - Validation: `name` required, max 100; `type` required enum.
  - Response: `{ id }`
- `PATCH /api/categories/:id`
  - Body: `{ name?, icon? }`
  - Partial update; `null` keeps the current value.
  - User can only edit/delete non-system categories they own.
- `DELETE /api/categories/:id`
  - Returns `{ success: true }`.
  - Fails if category is system or referenced by any transaction.

### Wallets
- `GET /api/wallets`
  - Returns `{ wallets, totalBalance }`.
  - `balance = initialBalance + incomeTransactions - expenseTransactions`.
- `GET /api/wallets/:id`
- `POST /api/wallets`
  - Body: `{ name, initialBalance, currency, icon? }`
- `PATCH /api/wallets/:id`
  - Body: `{ name?, icon? }`
  - `null` keeps the current value.
- `PUT /api/wallets/:id/config`
  - Body: `{ currency }`
  - Persists trimmed upper-case currency.
- `DELETE /api/wallets/:id`

### Transactions
- `GET /api/transactions`
  - Query: `walletIds`, `type`, `categoryIds`, `dateFrom`, `dateTo`, `exclude`, `page`, `limit`, `order`, `orderBy`.
  - Response: `{ items, page, limit, total }`.
  - Supports `exclude=Transfer` to omit transfer-backed transactions.
- `GET /api/transactions/:id`
- `GET /api/transactions/summary`
  - Excludes transfer transactions.
  - Response: `{ totalIncome, totalExpense, netBalance, byCategory }`.
- `POST /api/transactions`
  - Body: `{ walletId, amount, type, date, note?, categoryId }`
  - Validation: wallet/category must exist and be owned/visible.
- `PATCH /api/transactions/:id`
  - Body: `{ walletId?, amount?, type?, date?, note?, categoryId? }`
  - `null` keeps the current value.
  - Transfer-backed transactions cannot be edited.
- `DELETE /api/transactions/:id`
  - Transfer-backed transactions cannot be deleted directly.

### Transfers
- `GET /api/transfers`
  - Query: `walletId`, `dateFrom`, `dateTo`, `page`, `limit`, `order`, `orderBy`.
  - Response: `{ items, page, limit, total }`.
- `POST /api/transfers`
  - Body: `{ fromWalletId, toWalletId, amount, date, note? }`
  - Creates two transfer transactions:
    - source wallet expense, `isTransfer = true`
    - destination wallet income, `isTransfer = true`
  - Then creates transfer row linking both transaction ids.
- `DELETE /api/transfers/:id`
  - Deletes transfer plus its paired transactions.

## Validation and failures
- All finance routes require bearer auth.
- Ownership mismatches return `404` where the .NET handlers use not-found semantics.
- Category/system protection returns `403`.
- Create transaction wallet/category existence errors are field validation errors (`400` with `errors`).
- Update transaction wallet/category existence errors and transfer create wallet existence errors are message-based `400`s.
- Invalid JSON or invalid enum/date/uuid body values return validation `400`.

## Storage / schema
Add D1 tables and indexes for:
- `categories`
- `wallets`
- `transactions`
- `transfers`

Also seed the same default system categories as the .NET app so `GET /api/categories` behaves consistently.

## Tests
Add focused Vitest coverage proving:
- category CRUD + protection/in-use validation
- wallet CRUD/config + ownership
- transaction CRUD/list/summary + transfer exclusion
- transfer creation creates paired transactions and transfer deletion removes both
