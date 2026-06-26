# Gold Prices

Tracks "nhẫn tròn 9999 1 chỉ" buy/sell gold prices from four Vietnamese sources (SJC, PNJ, GiaVang, KimKhanhVietHung). Prices are scraped automatically every hour by a background service. A Create endpoint is provided for manual entry and testing.

All endpoints require authentication. Prices are global — not user-scoped.

**API routes:** `/api/gold-prices`

---

## Data Model

| Entity | Key Fields |
|---|---|
| `GoldPriceEntity` | `Source` (GoldSource enum: SJC / PNJ / GiaVang / KimKhanhVietHung), `BuyPrice` (decimal, VND), `SellPrice` (decimal, VND), `RecordedAt` (DateTime UTC) |

**Unique index:** `(Source, RecordedAt)` — prevents duplicate records for the same source at the same timestamp.

---

## Create Gold Price

**Endpoint:** `POST /api/gold-prices`
**Auth:** Required

### Use Case

As an authenticated user or background service, I want to record a gold price entry for a specific source and timestamp, so that price history is captured in the database.

### Description

1. Client sends `source` (required, one of: `"SJC"`, `"PNJ"`, `"GiaVang"`, `"KimKhanhVietHung"`), `buyPrice` (required, greater than 0), `sellPrice` (required, greater than 0), `recordedAt` (required, UTC DateTime).
2. A new `GoldPriceEntity` is created and saved.
3. Returns the new record's `Id`.

### Test Cases

| ID | Test | Expected |
|---|---|---|
| GOLDPRICE-CREATE-001 | `CreateGoldPrice_WithoutToken_ReturnsUnauthorized` | 401 |
| GOLDPRICE-CREATE-002 | `CreateGoldPrice_WithValidSjcData_ReturnsOkAndId` | ✅ 200 + non-empty Id |
| GOLDPRICE-CREATE-003 | `CreateGoldPrice_WithMissingSource_ReturnsBadRequest` | ✅ 400 |
| GOLDPRICE-CREATE-004 | `CreateGoldPrice_WithZeroBuyPrice_ReturnsBadRequest` | ✅ 400 |
| GOLDPRICE-CREATE-005 | `CreateGoldPrice_WithNegativeSellPrice_ReturnsBadRequest` | ✅ 400 |
| GOLDPRICE-CREATE-006 | `CreateGoldPrice_WithAllFourSources_ReturnsOk` | ✅ 200 × 4 |

---

## Get Gold Prices

**Endpoint:** `GET /api/gold-prices`
**Auth:** Required

### Use Case

As an authenticated user, I want to retrieve a paginated and filterable list of gold price records, so that I can view price history and build charts. I can also request the latest record per source to display a summary dashboard.

### Description

1. Client sends optional query parameters: `source` (GoldSource), `dateFrom` (DateTime), `dateTo` (DateTime), `latest` (bool), `page` (default 1), `limit` (default 20), `order` (Asc / Desc, default Desc), `orderBy` (RecordedAt / BuyPrice / SellPrice / Source, default RecordedAt).
2. If `latest=true`: returns one record per source (the most recent `RecordedAt` for each source). Pagination params are ignored; `page=1`, `limit=items.Count`, `total=items.Count`.
3. Otherwise: results are filtered by the provided parameters and paginated.
4. Returns `{ items, page, limit, total }`.

### Test Cases

| ID | Test | Expected |
|---|---|---|
| GOLDPRICE-GET-ALL-001 | `GetGoldPrices_WithoutToken_ReturnsUnauthorized` | 401 |
| GOLDPRICE-GET-ALL-002 | `GetGoldPrices_WithNoFilter_ReturnsPaginatedList` | ✅ 200 + paginated list |
| GOLDPRICE-GET-ALL-003 | `GetGoldPrices_FilterBySource_ReturnsOnlyMatchingSource` | ✅ 200 + all items Source=SJC |
| GOLDPRICE-GET-ALL-004 | `GetGoldPrices_FilterByDateRange_ReturnsMatchingRange` | ✅ 200 + items in range only |
| GOLDPRICE-GET-ALL-005 | `GetGoldPrices_WithPagination_ReturnsCorrectPage` | ✅ 200 + Items.Count=2, Total≥5 |
| GOLDPRICE-GET-ALL-006 | `GetGoldPrices_DefaultOrder_ReturnsNewestFirst` | ✅ 200 + newest RecordedAt first |
| GOLDPRICE-GET-ALL-007 | `GetGoldPrices_WithLatestTrue_ReturnsLatestRecordPerSource` | ✅ 200 + one item per source with latest prices |
| GOLDPRICE-GET-ALL-008 | `GetGoldPrices_WithLatestTrue_MultipleRecordsPerSource_ReturnsOnlyLatest` | ✅ 200 + only newest record per source |
| GOLDPRICE-GET-ALL-009 | `GetGoldPrices_WithLatestTrue_AllFiveSourcesSeeded_ReturnsOneItemPerSource` | ✅ 200 + exactly 5 items, one per source |

---

## Background Scraper

The `GoldPriceScraperService` (Phase 2) runs hourly using `PeriodicTimer` and scrapes from four sources:

| Source | URL | Gold Type |
|---|---|---|
| SJC | sjc.com.vn/xml/tygiavang.xml | Nhẫn tròn 9999 |
| PNJ | pnj.com.vn/blog/gia-vang/ | Nhẫn tròn 9999 |
| GiaVang | giavang.org/the-gioi | International spot → converted to VND/chỉ |
| KimKhanhVietHung | kimkhanhviethung.vn/tra-cuu-gia-vang.html | Nhẫn tròn 1 chỉ |

Each scraper failure is logged at `Warning` level; the service continues to the next source. A failed scrape does not write a partial record.
