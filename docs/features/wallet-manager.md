# Wallet Manager

A personal finance tracking mini-app inspired by Money Lover. Users can manage multiple wallets, record income and expense transactions, transfer money between wallets, and visualise their spending with charts.

All endpoints require authentication and are scoped to the authenticated user — users cannot see or modify each other's data.

**API routes:** Wallets → `/api/wallets` · Transactions → `/api/transactions` · Transfers → `/api/transfers` · Categories → `/api/categories` · Icons → `/api/icons`

---

## Data Model

| Entity              | Key Fields                                                                                                                       |
| ------------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| `WalletEntity`      | `Name`, `InitialBalance` (decimal), `Currency`, `Icon`, `UserId`                                                                 |
| `CategoryEntity`    | `Name`, `Type` (Income / Expense), `Icon`, `UserId` (null = system default), `IsSystem`                                          |
| `TransactionEntity` | `Amount` (decimal, always positive), `Type` (Income / Expense), `Date`, `Note`, `WalletId`, `CategoryId`, `UserId`, `IsTransfer` |
| `TransferEntity`    | `FromWalletId`, `ToWalletId`, `Amount`, `Date`, `Note`, `FromTransactionId`, `ToTransactionId`                                   |

**Wallet balance** is computed on demand: `Balance = InitialBalance + SUM(Income transactions) − SUM(Expense transactions)`. No stored balance field; no sync logic required.

---

## Create Wallet

**Endpoint:** `POST /api/wallets`
**Auth:** Required

### Use Case

As an authenticated user, I want to create a named wallet with an initial balance and currency, so that I can start tracking my finances separately per account (e.g. cash, bank, savings).

### Description

1. Client sends `name` (required, max 100 characters), `initialBalance` (required, default `0`), `currency` (required, e.g. `"USD"`), and optionally `icon` (emoji or icon name string).
2. The wallet is created and associated with the authenticated user.
3. Returns the new wallet's `Id`.

### Test Cases

| Test Case ID       | Description                                                                            | Status  |
| ------------------ | -------------------------------------------------------------------------------------- | ------- |
| WALLETS-CREATE-001 | Missing token returns 401 Unauthorized                                                 | ✅ Pass |
| WALLETS-CREATE-002 | Valid data creates wallet and returns 200 with Id                                      | ✅ Pass |
| WALLETS-CREATE-003 | Missing name returns 400 Bad Request                                                   | ✅ Pass |
| WALLETS-CREATE-004 | Missing currency returns 400 Bad Request                                               | ✅ Pass |
| WALLETS-CREATE-005 | Negative initial balance is allowed (e.g. overdraft accounts)                          | ✅ Pass |
| WALLETS-CREATE-006 | Created wallet's balance equals the provided initialBalance when no transactions exist | ✅ Pass |
| WALLETS-CREATE-007 | Created wallet is owned by the authenticated user                                      | ✅ Pass |

---

## Get Wallets

**Endpoint:** `GET /api/wallets`
**Auth:** Required

### Use Case

As an authenticated user, I want to retrieve all my wallets with their current balances and a grand total, so that I can see my overall financial position at a glance.

### Description

1. Client calls the endpoint with a valid access token.
2. Returns only wallets belonging to the authenticated user.
3. Response includes each wallet's `id`, `name`, `balance`, `currency`, `icon`, plus a computed `totalBalance` (sum of all wallet balances, regardless of currency).

### Test Cases

| Test Case ID        | Description                                                 | Status  |
| ------------------- | ----------------------------------------------------------- | ------- |
| WALLETS-GET-ALL-001 | Missing token returns 401 Unauthorized                      | ✅ Pass |
| WALLETS-GET-ALL-002 | Authenticated user receives their wallets and returns 200   | ✅ Pass |
| WALLETS-GET-ALL-003 | Only the current user's wallets are returned                | ✅ Pass |
| WALLETS-GET-ALL-004 | Response includes correct `totalBalance` across all wallets | ✅ Pass |
| WALLETS-GET-ALL-005 | User with no wallets receives an empty list                 | ✅ Pass |

---

## Get Wallet

**Endpoint:** `GET /api/wallets/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to retrieve the details of a specific wallet, so that I can view its current balance and metadata.

### Description

1. Client provides the wallet `id` (route).
2. If the wallet doesn't exist or belongs to a different user → `404 Not Found`.
3. Returns the wallet's full details.

### Test Cases

| Test Case ID    | Description                                            | Status  |
| --------------- | ------------------------------------------------------ | ------- |
| WALLETS-GET-001 | Missing token returns 401 Unauthorized                 | ✅ Pass |
| WALLETS-GET-002 | Valid owned wallet id returns 200 with wallet data     | ✅ Pass |
| WALLETS-GET-003 | Non-existent wallet id returns 404 Not Found           | ✅ Pass |
| WALLETS-GET-004 | Requesting another user's wallet returns 404 Not Found | ✅ Pass |

---

## Update Wallet

**Endpoint:** `PUT /api/wallets/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to rename or change the icon of one of my wallets, so that I can keep it organised.

### Description

1. Client sends the wallet `id` (route) and `name` (required), optional `icon`.
2. If the wallet doesn't exist or belongs to a different user → `404 Not Found`.
3. Only metadata fields (`name`, `icon`) are updated.
4. Returns the updated wallet's `Id`.

### Test Cases

| Test Case ID       | Description                                          | Status  |
| ------------------ | ---------------------------------------------------- | ------- |
| WALLETS-UPDATE-001 | Missing token returns 401 Unauthorized               | ✅ Pass |
| WALLETS-UPDATE-002 | Valid update returns 200                             | ✅ Pass |
| WALLETS-UPDATE-003 | Missing name returns 400 Bad Request                 | ✅ Pass |
| WALLETS-UPDATE-004 | Updating another user's wallet returns 404 Not Found | ✅ Pass |
| WALLETS-UPDATE-005 | Non-existent wallet id returns 404 Not Found         | ✅ Pass |
| WALLETS-UPDATE-006 | Update does not affect the computed wallet balance   | ✅ Pass |

---

## Delete Wallet

**Endpoint:** `DELETE /api/wallets/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to delete one of my wallets, so that I can remove accounts I no longer use.

### Description

1. Client provides the wallet `id` (route).
2. If the wallet doesn't exist or belongs to a different user → `404 Not Found`.
3. The wallet and all its associated transactions are deleted. Any transfer records that reference this wallet are also deleted.

### Test Cases

| Test Case ID       | Description                                          | Status  |
| ------------------ | ---------------------------------------------------- | ------- |
| WALLETS-DELETE-001 | Missing token returns 401 Unauthorized               | ✅ Pass |
| WALLETS-DELETE-002 | Existing owned wallet is deleted and returns 200     | ✅ Pass |
| WALLETS-DELETE-003 | Deleting another user's wallet returns 404 Not Found | ✅ Pass |
| WALLETS-DELETE-004 | Non-existent wallet id returns 404 Not Found         | ✅ Pass |
| WALLETS-DELETE-005 | Deleting wallet also removes all its transactions    | ✅ Pass |

---

## Get Wallet Summary

**Endpoint:** `GET /api/wallets/summary`
**Auth:** Required

### Use Case

As an authenticated user, I want a quick financial overview — total balance, total income and total expense for the current month — so that I can monitor my finances from the dashboard.

### Description

1. Client calls the endpoint with a valid access token.
2. Returns: `totalBalance` (sum of all wallet balances), `monthlyIncome` (current month income across all wallets), `monthlyExpense` (current month expense across all wallets), and `recentTransactions` (last 5 transactions across all wallets).

### Test Cases

| Test Case ID        | Description                                                          | Status  |
| ------------------- | -------------------------------------------------------------------- | ------- |
| WALLETS-SUMMARY-001 | Missing token returns 401 Unauthorized                               | ✅ Pass |
| WALLETS-SUMMARY-002 | Returns correct totalBalance reflecting all wallet balances          | ✅ Pass |
| WALLETS-SUMMARY-003 | Monthly income and expense reflect current month's transactions only | ✅ Pass |
| WALLETS-SUMMARY-004 | Recent transactions contains at most 5 entries                       | ✅ Pass |
| WALLETS-SUMMARY-005 | User with no wallets returns all zeros and empty recent transactions | ✅ Pass |

---

## Get Categories

**Endpoint:** `GET /api/categories`
**Auth:** Required

### Use Case

As an authenticated user, I want to retrieve the list of transaction categories, so that I can assign categories when creating transactions.

### Description

1. Client calls the endpoint with optional `type` filter (`Income` or `Expense`).
2. Returns system-default categories (seeded, `IsSystem = true`) plus the current user's custom categories.
3. Each category includes `id`, `name`, `type`, `icon`, `isSystem`.

### Test Cases

| Test Case ID | Description                                                | Status  |
| ------------ | ---------------------------------------------------------- | ------- |
| CATS-GET-001 | Missing token returns 401 Unauthorized                     | ✅ Pass |
| CATS-GET-002 | Returns system categories and user's own custom categories | ✅ Pass |
| CATS-GET-003 | Does not return another user's custom categories           | ✅ Pass |
| CATS-GET-004 | Filter by type=Income returns only Income categories       | ✅ Pass |
| CATS-GET-005 | Filter by type=Expense returns only Expense categories     | ✅ Pass |

---

## Create Category

**Endpoint:** `POST /api/categories`
**Auth:** Required

### Use Case

As an authenticated user, I want to create a custom transaction category, so that I can organise my transactions beyond the default categories.

### Description

1. Client sends `name` (required, max 100 characters), `type` (required: `Income` or `Expense`), and optionally `icon`.
2. The category is created as a user-owned (non-system) category.
3. Returns the new category's `Id`.

### Test Cases

| Test Case ID    | Description                                                               | Status  |
| --------------- | ------------------------------------------------------------------------- | ------- |
| CATS-CREATE-001 | Missing token returns 401 Unauthorized                                    | ✅ Pass |
| CATS-CREATE-002 | Valid data creates category and returns 200 with Id                       | ✅ Pass |
| CATS-CREATE-003 | Missing name returns 400 Bad Request                                      | ✅ Pass |
| CATS-CREATE-004 | Missing type returns 400 Bad Request                                      | ✅ Pass |
| CATS-CREATE-005 | Invalid type value returns 400 Bad Request                                | ✅ Pass |
| CATS-CREATE-006 | Created category is owned by the authenticated user and IsSystem is false | ✅ Pass |

---

## Update Category

**Endpoint:** `PUT /api/categories/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to rename or change the icon of one of my custom categories, so that I can keep my organisation up to date.

### Description

1. Client sends the category `id` (route) and `name` (required), optional `icon`.
2. If the category doesn't exist or belongs to a different user → `404 Not Found`.
3. If the category is a system category (`IsSystem = true`) → `403 Forbidden`.
4. Returns the updated category's `Id`.

### Test Cases

| Test Case ID    | Description                                            | Status  |
| --------------- | ------------------------------------------------------ | ------- |
| CATS-UPDATE-001 | Missing token returns 401 Unauthorized                 | ✅ Pass |
| CATS-UPDATE-002 | Valid update returns 200                               | ✅ Pass |
| CATS-UPDATE-003 | Missing name returns 400 Bad Request                   | ✅ Pass |
| CATS-UPDATE-004 | Updating a system category returns 403 Forbidden       | ✅ Pass |
| CATS-UPDATE-005 | Updating another user's category returns 404 Not Found | ✅ Pass |
| CATS-UPDATE-006 | Non-existent category id returns 404 Not Found         | ✅ Pass |

---

## Delete Category

**Endpoint:** `DELETE /api/categories/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to delete one of my custom categories, so that I can remove categories I no longer use.

### Description

1. Client provides the category `id` (route).
2. If the category doesn't exist or belongs to a different user → `404 Not Found`.
3. If the category is a system category → `403 Forbidden`.
4. If any transaction references this category → `400 Bad Request` with message `"Category is in use by transactions"`.
5. The category is deleted.

### Test Cases

| Test Case ID    | Description                                                                                           | Status  |
| --------------- | ----------------------------------------------------------------------------------------------------- | ------- |
| CATS-DELETE-001 | Missing token returns 401 Unauthorized                                                                | ✅ Pass |
| CATS-DELETE-002 | Existing owned category (with no transactions) is deleted and returns 200                             | ✅ Pass |
| CATS-DELETE-003 | Deleting a system category returns 403 Forbidden                                                      | ✅ Pass |
| CATS-DELETE-004 | Deleting another user's category returns 404 Not Found                                                | ✅ Pass |
| CATS-DELETE-005 | Non-existent category id returns 404 Not Found                                                        | ✅ Pass |
| CATS-DELETE-006 | Category referenced by one or more transactions returns 400 with "Category is in use by transactions" | ✅ Pass |

---

## Create Transaction

**Endpoint:** `POST /api/transactions`
**Auth:** Required

### Use Case

As an authenticated user, I want to record an income or expense transaction on one of my wallets, so that my wallet balance stays accurate and I can track where my money goes.

### Description

1. Client sends `walletId` (required), `amount` (required, positive decimal), `type` (required: `Income` or `Expense`), `date` (required), optional `note`, optional `categoryId`.
2. If `walletId` doesn't exist or belongs to a different user → `404 Not Found`.
3. If `categoryId` is provided but doesn't exist or belongs to a different user → `404 Not Found`.
4. The transaction is created and associated with the authenticated user.
5. Returns the new transaction's `Id`.

### Test Cases

| Test Case ID   | Description                                                                         | Status  |
| -------------- | ----------------------------------------------------------------------------------- | ------- |
| TXN-CREATE-001 | Missing token returns 401 Unauthorized                                              | ✅ Pass |
| TXN-CREATE-002 | Valid income transaction created; wallet computed balance reflects the new income   | ✅ Pass |
| TXN-CREATE-003 | Valid expense transaction created; wallet computed balance reflects the new expense | ✅ Pass |
| TXN-CREATE-004 | Missing walletId returns 400 Bad Request                                            | ✅ Pass |
| TXN-CREATE-005 | Missing amount returns 400 Bad Request                                              | ✅ Pass |
| TXN-CREATE-006 | Zero or negative amount returns 400 Bad Request                                     | ✅ Pass |
| TXN-CREATE-007 | Missing date returns 400 Bad Request                                                | ✅ Pass |
| TXN-CREATE-008 | Missing type returns 400 Bad Request                                                | ✅ Pass |
| TXN-CREATE-009 | Non-existent or another user's walletId returns 404 Not Found                       | ✅ Pass |
| TXN-CREATE-010 | Non-existent or another user's categoryId returns 404 Not Found                     | ✅ Pass |
| TXN-CREATE-011 | Transaction without a category is allowed (categoryId optional)                     | ✅ Pass |

---

## Get Transactions

**Endpoint:** `GET /api/transactions`
**Auth:** Required

### Use Case

As an authenticated user, I want to list my transactions with filtering and pagination, so that I can browse and analyse my transaction history.

### Description

1. Client sends optional filters: `walletId`, `type`, `categoryId`, `dateFrom`, `dateTo`, plus paging parameters (`page`, `pageSize`).
2. Returns only transactions belonging to the authenticated user.
3. Results are ordered by `date` descending, then by `CreatedOn` descending.
4. Each item includes wallet name and category name for display.

### Test Cases

| Test Case ID    | Description                                                               | Status  |
| --------------- | ------------------------------------------------------------------------- | ------- |
| TXN-GET-ALL-001 | Missing token returns 401 Unauthorized                                    | ✅ Pass |
| TXN-GET-ALL-002 | Authenticated user receives their transactions and returns 200            | ✅ Pass |
| TXN-GET-ALL-003 | Only the current user's transactions are returned                         | ✅ Pass |
| TXN-GET-ALL-004 | Filter by walletId returns only transactions for that wallet              | ✅ Pass |
| TXN-GET-ALL-005 | Filter by type=Income returns only income transactions                    | ✅ Pass |
| TXN-GET-ALL-006 | Filter by type=Expense returns only expense transactions                  | ✅ Pass |
| TXN-GET-ALL-007 | Filter by dateFrom and dateTo returns only transactions within that range | ✅ Pass |
| TXN-GET-ALL-008 | Filter by categoryId returns only transactions in that category           | ✅ Pass |
| TXN-GET-ALL-009 | Pagination parameters return the correct subset                           | ✅ Pass |

---

## Get Transaction

**Endpoint:** `GET /api/transactions/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to retrieve the details of a specific transaction, so that I can review its full information.

### Description

1. Client provides the transaction `id` (route).
2. If the transaction doesn't exist or belongs to a different user → `404 Not Found`.
3. Returns full transaction details including wallet name, category name, and whether it is part of a transfer.

### Test Cases

| Test Case ID | Description                                                 | Status  |
| ------------ | ----------------------------------------------------------- | ------- |
| TXN-GET-001  | Missing token returns 401 Unauthorized                      | ✅ Pass |
| TXN-GET-002  | Valid id returns 200 with correct data                      | ✅ Pass |
| TXN-GET-003  | Non-existent id returns 404 Not Found                       | ✅ Pass |
| TXN-GET-004  | Requesting another user's transaction returns 404 Not Found | ✅ Pass |

---

## Update Transaction

**Endpoint:** `PUT /api/transactions/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to edit a transaction's amount, type, date, note, or category, so that I can correct mistakes or add missing details.

### Description

1. Client sends the transaction `id` (route) and updated `amount` (required), `type` (required), `date` (required), optional `note`, optional `categoryId`.
2. If the transaction doesn't exist or belongs to a different user → `404 Not Found`.
3. If the transaction is part of a transfer → `400 Bad Request` (edit the transfer instead).
4. The transaction fields are updated. The wallet's computed balance automatically reflects the change on next query.
5. Returns the updated transaction's `Id`.

### Test Cases

| Test Case ID   | Description                                                                             | Status  |
| -------------- | --------------------------------------------------------------------------------------- | ------- |
| TXN-UPDATE-001 | Missing token returns 401 Unauthorized                                                  | ✅ Pass |
| TXN-UPDATE-002 | Valid update returns 200                                                                | ✅ Pass |
| TXN-UPDATE-003 | After changing amount, computed wallet balance reflects the updated transaction         | ✅ Pass |
| TXN-UPDATE-004 | After changing type from Income to Expense, computed wallet balance reflects the change | ✅ Pass |
| TXN-UPDATE-005 | Zero or negative amount returns 400 Bad Request                                         | ✅ Pass |
| TXN-UPDATE-006 | Non-existent transaction id returns 404 Not Found                                       | ✅ Pass |
| TXN-UPDATE-007 | Updating another user's transaction returns 404 Not Found                               | ✅ Pass |
| TXN-UPDATE-008 | Updating a transfer transaction returns 400 Bad Request                                 | ✅ Pass |

---

## Delete Transaction

**Endpoint:** `DELETE /api/transactions/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to delete an incorrect transaction, so that it no longer affects my wallet balance.

### Description

1. Client provides the transaction `id` (route).
2. If the transaction doesn't exist or belongs to a different user → `404 Not Found`.
3. If the transaction is part of a transfer → `400 Bad Request` (delete the transfer instead).
4. The transaction is removed. The wallet's computed balance automatically reflects the deletion on next query.

### Test Cases

| Test Case ID   | Description                                                           | Status  |
| -------------- | --------------------------------------------------------------------- | ------- |
| TXN-DELETE-001 | Missing token returns 401 Unauthorized                                | ✅ Pass |
| TXN-DELETE-002 | Deleting an income transaction reduces the computed wallet balance    | ✅ Pass |
| TXN-DELETE-003 | Deleting an expense transaction increases the computed wallet balance | ✅ Pass |
| TXN-DELETE-004 | Deleting a transfer transaction returns 400 Bad Request               | ✅ Pass |
| TXN-DELETE-005 | Non-existent transaction id returns 404 Not Found                     | ✅ Pass |
| TXN-DELETE-006 | Deleting another user's transaction returns 404 Not Found             | ✅ Pass |

---

## Get Transaction Summary

**Endpoint:** `GET /api/transactions/summary`
**Auth:** Required

### Use Case

As an authenticated user, I want to see an income vs expense summary for a given period (optionally filtered by wallet), so that I can understand my spending patterns.

### Description

1. Client sends optional `walletId`, `dateFrom`, `dateTo`.
2. Returns `totalIncome`, `totalExpense`, `netBalance` (income − expense) for the period.
3. Returns a `byCategory` breakdown: each entry has `categoryId`, `categoryName`, `type`, `total` — for rendering a pie chart.

### Test Cases

| Test Case ID    | Description                                                           | Status  |
| --------------- | --------------------------------------------------------------------- | ------- |
| TXN-SUMMARY-001 | Missing token returns 401 Unauthorized                                | ✅ Pass |
| TXN-SUMMARY-002 | Returns correct totalIncome and totalExpense for the period           | ✅ Pass |
| TXN-SUMMARY-003 | netBalance equals totalIncome minus totalExpense                      | ✅ Pass |
| TXN-SUMMARY-004 | Filter by walletId limits totals to that wallet                       | ✅ Pass |
| TXN-SUMMARY-005 | Filter by dateFrom and dateTo limits totals to that range             | ✅ Pass |
| TXN-SUMMARY-006 | byCategory list correctly groups and totals transactions per category | ✅ Pass |
| TXN-SUMMARY-007 | Only the current user's transactions are included                     | ✅ Pass |

---

## Create Transfer

**Endpoint:** `POST /api/transfers`
**Auth:** Required

### Use Case

As an authenticated user, I want to record a transfer of money between two of my wallets, so that it is not counted as income or expense and both wallet balances are correctly computed.

### Description

1. Client sends `fromWalletId` (required), `toWalletId` (required), `amount` (required, positive), `date` (required), optional `note`.
2. `fromWalletId` and `toWalletId` must be different → `400 Bad Request` otherwise.
3. Both wallets must exist and belong to the current user → `404 Not Found` otherwise.
4. Two linked transactions are created internally: an Expense in `fromWallet` and an Income in `toWallet`. Both are flagged as transfer transactions (not editable or deletable directly).
5. A `TransferEntity` record is created linking both transactions.
6. Returns the new transfer's `Id`.

### Test Cases

| Test Case ID        | Description                                                                                           | Status  |
| ------------------- | ----------------------------------------------------------------------------------------------------- | ------- |
| TRANSFER-CREATE-001 | Missing token returns 401 Unauthorized                                                                | ✅ Pass |
| TRANSFER-CREATE-002 | Valid transfer created; fromWallet computed balance decreases and toWallet computed balance increases | ✅ Pass |
| TRANSFER-CREATE-003 | fromWalletId same as toWalletId returns 400 Bad Request                                               | ✅ Pass |
| TRANSFER-CREATE-004 | Non-existent or another user's fromWalletId returns 404 Not Found                                     | ✅ Pass |
| TRANSFER-CREATE-005 | Non-existent or another user's toWalletId returns 404 Not Found                                       | ✅ Pass |
| TRANSFER-CREATE-006 | Zero or negative amount returns 400 Bad Request                                                       | ✅ Pass |
| TRANSFER-CREATE-007 | Missing date returns 400 Bad Request                                                                  | ✅ Pass |
| TRANSFER-CREATE-008 | Two linked transactions are created (one Income, one Expense), both flagged IsTransfer                | ✅ Pass |

---

## Get Transfers

**Endpoint:** `GET /api/transfers`
**Auth:** Required

### Use Case

As an authenticated user, I want to view my transfer history, so that I can track money movements between my wallets.

### Description

1. Client sends optional `walletId` (filter by either from or to wallet), `dateFrom`, `dateTo`, plus paging parameters.
2. Returns only transfers belonging to the authenticated user.
3. Results are ordered by `date` descending.
4. Each item includes from/to wallet names and amount.

### Test Cases

| Test Case ID     | Description                                                                           | Status  |
| ---------------- | ------------------------------------------------------------------------------------- | ------- |
| TRANSFER-GET-001 | Missing token returns 401 Unauthorized                                                | ✅ Pass |
| TRANSFER-GET-002 | Authenticated user receives their transfers and returns 200                           | ✅ Pass |
| TRANSFER-GET-003 | Only the current user's transfers are returned                                        | ✅ Pass |
| TRANSFER-GET-004 | Filter by walletId returns transfers involving that wallet (as source or destination) | ✅ Pass |
| TRANSFER-GET-005 | Filter by date range returns only transfers within that range                         | ✅ Pass |
| TRANSFER-GET-006 | Pagination parameters return the correct subset                                       | ✅ Pass |

---

## Delete Transfer

**Endpoint:** `DELETE /api/transfers/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to delete an incorrect transfer, so that the linked transactions are removed and both wallet balances are recomputed correctly.

### Description

1. Client provides the transfer `id` (route).
2. If the transfer doesn't exist or belongs to a different user → `404 Not Found`.
3. Both linked transactions are deleted. The wallet computed balances automatically reflect the deletion on next query.

### Test Cases

| Test Case ID        | Description                                                                                 | Status  |
| ------------------- | ------------------------------------------------------------------------------------------- | ------- |
| TRANSFER-DELETE-001 | Missing token returns 401 Unauthorized                                                      | ✅ Pass |
| TRANSFER-DELETE-002 | Valid delete removes both linked transactions; wallet computed balances reflect the removal | ✅ Pass |
| TRANSFER-DELETE-003 | Non-existent transfer id returns 404 Not Found                                              | ✅ Pass |
| TRANSFER-DELETE-004 | Deleting another user's transfer returns 404 Not Found                                      | ✅ Pass |
| TRANSFER-DELETE-005 | Both linked transactions are removed after deletion                                         | ✅ Pass |

---

## Get Icons

**Endpoint:** `GET /api/icons`
**Auth:** Required

### Use Case

As an authenticated user, I want to retrieve the available icons so that I can pick one when creating or editing a wallet or category.

### Description

1. Client calls the endpoint with an optional `group` query parameter (`Wallet`, `Category`, or `General`).
2. If `group` is provided, returns icons of that group plus all `General` icons. If `group` is omitted or unrecognised, returns all icons.
3. Results are ordered by `SortOrder` ascending within each group.
4. Each icon includes `id`, `value`, `name`, `group`, `sortOrder`, `isSystem`.

### Test Cases

| Test Case ID  | Description                                                         | Status  |
| ------------- | ------------------------------------------------------------------- | ------- |
| ICONS-GET-001 | Missing token returns 401 Unauthorized                              | ✅ Pass |
| ICONS-GET-002 | Authenticated user receives all seeded icons                        | ✅ Pass |
| ICONS-GET-003 | Filter by group returns only icons of that group plus General icons | ✅ Pass |
| ICONS-GET-004 | Unrecognised group value returns all icons                          | ✅ Pass |
| ICONS-GET-005 | Icons within a group are ordered by sortOrder ascending             | ✅ Pass |

---

## Create Icon

**Endpoint:** `POST /api/icons`
**Auth:** Required

### Use Case

As an authenticated user, I want to add a custom icon so that wallets and categories can display it.

### Description

1. Client sends `value` (required, max 2000 characters — emoji, SVG markup, or image URL), `name` (required, max 100 characters), `group` (required: `Wallet`, `Category`, or `General`), and `sortOrder` (required).
2. The icon is created with `IsSystem = false`.
3. Returns the new icon's `Id`.

### Test Cases

| Test Case ID     | Description                                             | Status  |
| ---------------- | ------------------------------------------------------- | ------- |
| ICONS-CREATE-001 | Missing token returns 401 Unauthorized                  | ✅ Pass |
| ICONS-CREATE-002 | Valid request returns 200 with Id                       | ✅ Pass |
| ICONS-CREATE-003 | Empty value returns 400 Bad Request                     | ✅ Pass |
| ICONS-CREATE-004 | Empty name returns 400 Bad Request                      | ✅ Pass |
| ICONS-CREATE-005 | Value exceeding 2000 characters returns 400 Bad Request | ✅ Pass |
| ICONS-CREATE-006 | Name exceeding 100 characters returns 400 Bad Request   | ✅ Pass |
| ICONS-CREATE-007 | Created icon appears in GET /icons response             | ✅ Pass |

---

## Update Icon

**Endpoint:** `PUT /api/icons/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to edit a custom icon's value, name, group, or sort order so that I can correct or reorganise it.

### Description

1. Client sends the icon `id` (route) and updated `value`, `name`, `group`, `sortOrder`.
2. If the icon doesn't exist → `404 Not Found`.
3. If the icon is a system icon (`IsSystem = true`) → `403 Forbidden`.
4. Returns the updated icon's `Id`.

### Test Cases

| Test Case ID     | Description                                       | Status  |
| ---------------- | ------------------------------------------------- | ------- |
| ICONS-UPDATE-001 | Missing token returns 401 Unauthorized            | ✅ Pass |
| ICONS-UPDATE-002 | Valid update returns 200                          | ✅ Pass |
| ICONS-UPDATE-003 | Changes are persisted and reflected in GET /icons | ✅ Pass |
| ICONS-UPDATE-004 | Non-existent icon id returns 404 Not Found        | ✅ Pass |
| ICONS-UPDATE-005 | Updating a system icon returns 403 Forbidden      | ✅ Pass |
| ICONS-UPDATE-006 | Empty value returns 400 Bad Request               | ✅ Pass |

---

## Delete Icon

**Endpoint:** `DELETE /api/icons/{id}`
**Auth:** Required

### Use Case

As an authenticated user, I want to delete a custom icon I no longer need.

### Description

1. Client provides the icon `id` (route).
2. If the icon doesn't exist → `404 Not Found`.
3. If the icon is a system icon → `403 Forbidden`.
4. The icon is removed.

### Test Cases

| Test Case ID     | Description                                   | Status  |
| ---------------- | --------------------------------------------- | ------- |
| ICONS-DELETE-001 | Missing token returns 401 Unauthorized        | ✅ Pass |
| ICONS-DELETE-002 | Valid delete returns 200 with `success: true` | ✅ Pass |
| ICONS-DELETE-003 | Deleted icon no longer appears in GET /icons  | ✅ Pass |
| ICONS-DELETE-004 | Non-existent icon id returns 404 Not Found    | ✅ Pass |
| ICONS-DELETE-005 | Deleting a system icon returns 403 Forbidden  | ✅ Pass |

---

## Upload Icon Image

**Endpoint:** `POST /api/icons/upload-image`
**Auth:** Required

### Use Case

As an authenticated user, I want to upload an image file as an icon so that I can use a custom image in the icon picker.

### Description

1. Client sends `fileName` (required) with a supported extension: `jpg`, `jpeg`, `png`, `gif`, `webp`, or `svg`.
2. The API generates a presigned upload URL pointing to Cloudflare R2 storage and returns the final public `iconUrl` and `contentType`.
3. Client uploads the file directly to the presigned URL (PUT request), then uses the returned `iconUrl` as the icon `value` when creating an icon.

### Test Cases

| Test Case ID     | Description                                                 | Status  |
| ---------------- | ----------------------------------------------------------- | ------- |
| ICONS-UPLOAD-001 | Missing token returns 401 Unauthorized                      | ✅ Pass |
| ICONS-UPLOAD-002 | Empty fileName returns 400 Bad Request                      | ✅ Pass |
| ICONS-UPLOAD-003 | Unsupported extension (e.g. `.bmp`) returns 400 Bad Request | ✅ Pass |
| ICONS-UPLOAD-004 | fileName with no extension returns 400 Bad Request          | ✅ Pass |
