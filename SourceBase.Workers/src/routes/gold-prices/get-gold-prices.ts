import { z } from "zod";
import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import { ValidationError, validationErrorFromZod } from "../../lib/errors";
import type { AppEnv } from "../../types";

const goldSources = ["SJC", "PNJ", "GiaVang", "KimKhanhVietHung", "NgocThinh"] as const;
const optionalPositiveInt = z.preprocess(value => (value === undefined ? undefined : Number(value)), z.number().int().positive().optional());
const querySchema = z.object({
  source: z.enum(goldSources).optional(),
  dateFrom: z.string().optional(),
  dateTo: z.string().optional(),
  latest: z.enum(["true", "false"]).optional(),
  page: optionalPositiveInt,
  limit: optionalPositiveInt,
  order: z.enum(["Asc", "Desc"]).optional(),
  orderBy: z.enum(["RecordedAt", "BuyPrice", "SellPrice", "Source"]).optional()
});

type GoldPriceRow = {
  id: string;
  source: string;
  buy_price: number;
  sell_price: number;
  recorded_at: string;
};

export function registerGetGoldPricesRoute(router: Hono<AppEnv>) {
  router.get("/", requireAuth, async c => {
    const parsedQuery = querySchema.safeParse(c.req.query());
    if (!parsedQuery.success) validationErrorFromZod(parsedQuery);
    const query = parsedQuery.data!;

    const page = query.page ?? 1;
    const limit = query.limit ?? 20;
    const order = query.order ?? "Desc";
    const orderBy = query.orderBy ?? "RecordedAt";
    const dateFrom = parseDate(query.dateFrom, "dateFrom");
    const dateTo = parseDate(query.dateTo, "dateTo");
    const latest = query.latest === "true";
    const rows = query.source
      ? await c.env.DB.prepare(`SELECT id, source, buy_price, sell_price, recorded_at FROM gold_prices WHERE source = ?`).bind(query.source).all<GoldPriceRow>()
      : await c.env.DB.prepare(`SELECT id, source, buy_price, sell_price, recorded_at FROM gold_prices`).all<GoldPriceRow>();

    let items = rows.results;
    if (latest) items = selectLatestBySource(items);
    if (dateFrom) items = items.filter(item => new Date(item.recorded_at).getTime() >= dateFrom.getTime());
    if (dateTo) items = items.filter(item => new Date(item.recorded_at).getTime() <= dateTo.getTime());

    const ordered = [...items].sort((left, right) => compareRows(left, right, orderBy, order));
    if (latest) {
      return c.json({
        items: ordered.map(toResponse),
        page: 1,
        limit: ordered.length,
        total: ordered.length
      });
    }

    return c.json({
      items: ordered.slice((page - 1) * limit, (page - 1) * limit + limit).map(toResponse),
      page,
      limit,
      total: ordered.length
    });
  });
}

function selectLatestBySource(rows: GoldPriceRow[]) {
  const latest = new Map<string, GoldPriceRow>();
  for (const row of rows) {
    const current = latest.get(row.source);
    if (!current || new Date(row.recorded_at).getTime() > new Date(current.recorded_at).getTime()) latest.set(row.source, row);
  }
  return [...latest.values()];
}

function compareRows(left: GoldPriceRow, right: GoldPriceRow, orderBy: "RecordedAt" | "BuyPrice" | "SellPrice" | "Source", order: "Asc" | "Desc") {
  const direction = order === "Desc" ? -1 : 1;
  const leftValue = getComparableValue(left, orderBy);
  const rightValue = getComparableValue(right, orderBy);
  if (leftValue < rightValue) return -1 * direction;
  if (leftValue > rightValue) return 1 * direction;
  return left.id.localeCompare(right.id);
}

function getComparableValue(row: GoldPriceRow, orderBy: "RecordedAt" | "BuyPrice" | "SellPrice" | "Source") {
  switch (orderBy) {
    case "BuyPrice":
      return Number(row.buy_price);
    case "SellPrice":
      return Number(row.sell_price);
    case "Source":
      return row.source;
    case "RecordedAt":
    default:
      return new Date(row.recorded_at).getTime();
  }
}

function toResponse(row: GoldPriceRow) {
  return {
    id: row.id,
    source: row.source,
    buyPrice: Number(row.buy_price),
    sellPrice: Number(row.sell_price),
    recordedAt: row.recorded_at
  };
}

function parseDate(value: string | undefined, field: "dateFrom" | "dateTo") {
  if (!value) return undefined;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) throw new ValidationError({ [field]: ["Invalid date."] });
  return date;
}
