import { beforeEach, describe, expect, it } from "vitest";
import { authHeaders, createTestContext, getAdminAccessToken, send, type TestContext } from "./helpers";

describe("SourceBase.Workers gold prices routes", () => {
  let context: TestContext;
  let token: string;

  beforeEach(async () => {
    context = await createTestContext();
    token = await getAdminAccessToken(context);
  });

  it("requires auth for gold price reads", async () => {
    const response = await send(context, "http://localhost/api/gold-prices");
    expect(response.status).toBe(401);
  });

  it("creates gold prices, floors recordedAt to the hour, and upserts duplicate source-hour rows", async () => {
    const firstResponse = await send(context, "http://localhost/api/gold-prices", {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify({
        items: [{ source: "PNJ", buyPrice: 1800000, sellPrice: 1850000, recordedAt: "2019-05-10T14:45:30.000Z" }]
      })
    });
    const firstBody = await firstResponse.json() as { ids: string[] };

    const secondResponse = await send(context, "http://localhost/api/gold-prices", {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify({
        items: [{ source: "PNJ", buyPrice: 1900000, sellPrice: 1950000, recordedAt: "2019-05-10T14:10:00.000Z" }]
      })
    });
    const secondBody = await secondResponse.json() as { ids: string[] };

    expect(firstResponse.status).toBe(200);
    expect(secondResponse.status).toBe(200);
    expect(secondBody.ids[0]).toBe(firstBody.ids[0]);

    const row = await context.bindings.DB.prepare(
      `SELECT buy_price, sell_price, recorded_at
       FROM gold_prices
       WHERE id = ?`
    )
      .bind(firstBody.ids[0])
      .first<{ buy_price: number; sell_price: number; recorded_at: string }>();

    expect(row).toEqual({
      buy_price: 1900000,
      sell_price: 1950000,
      recorded_at: "2019-05-10T14:00:00.000Z"
    });
  });

  it("validates create payloads", async () => {
    const response = await send(context, "http://localhost/api/gold-prices", {
      method: "POST",
      headers: authHeaders(token),
      body: JSON.stringify({
        items: [{ source: "SJC", buyPrice: 0, sellPrice: 1950000, recordedAt: "2026-01-01T00:00:00.000Z" }]
      })
    });

    expect(response.status).toBe(400);
  });

  it("filters and paginates gold prices", async () => {
    await seedGoldPrice(context, token, { source: "SJC", buyPrice: 1800000, sellPrice: 1850000, recordedAt: "2022-01-01T00:00:00.000Z" });
    await seedGoldPrice(context, token, { source: "PNJ", buyPrice: 1900000, sellPrice: 1950000, recordedAt: "2023-01-01T00:00:00.000Z" });
    await seedGoldPrice(context, token, { source: "GiaVang", buyPrice: 2000000, sellPrice: 2050000, recordedAt: "2024-01-01T00:00:00.000Z" });

    const response = await send(context, "http://localhost/api/gold-prices?page=2&limit=1&order=Asc&orderBy=BuyPrice&dateFrom=2022-01-01T00:00:00.000Z", {
      headers: authHeaders(token)
    });

    expect(response.status).toBe(200);
    const body = await response.json() as {
      items: Array<{ source: string; buyPrice: number }>;
      page: number;
      limit: number;
      total: number;
    };

    expect(body.page).toBe(2);
    expect(body.limit).toBe(1);
    expect(body.total).toBe(3);
    expect(body.items).toHaveLength(1);
    expect(body.items[0]).toMatchObject({ source: "PNJ", buyPrice: 1900000 });
  });

  it("returns only the latest row per source when latest=true", async () => {
    await seedGoldPrice(context, token, { source: "SJC", buyPrice: 1800000, sellPrice: 1850000, recordedAt: "2043-05-01T06:00:00.000Z" });
    await seedGoldPrice(context, token, { source: "SJC", buyPrice: 2000000, sellPrice: 2050000, recordedAt: "2043-05-01T07:00:00.000Z" });
    await seedGoldPrice(context, token, { source: "PNJ", buyPrice: 1880000, sellPrice: 1930000, recordedAt: "2043-05-01T06:00:00.000Z" });

    const response = await send(context, "http://localhost/api/gold-prices?latest=true&page=99&limit=1", {
      headers: authHeaders(token)
    });

    expect(response.status).toBe(200);
    const body = await response.json() as {
      items: Array<{ id: string; source: string; buyPrice: number; sellPrice: number; recordedAt: string }>;
      page: number;
      limit: number;
      total: number;
    };

    expect(body.page).toBe(1);
    expect(body.limit).toBe(2);
    expect(body.total).toBe(2);
    expect(body.items).toEqual([
      { id: expect.any(String), source: "SJC", buyPrice: 2000000, sellPrice: 2050000, recordedAt: "2043-05-01T07:00:00.000Z" },
      { id: expect.any(String), source: "PNJ", buyPrice: 1880000, sellPrice: 1930000, recordedAt: "2043-05-01T06:00:00.000Z" }
    ]);
  });
});

async function seedGoldPrice(context: TestContext, token: string, item: { source: string; buyPrice: number; sellPrice: number; recordedAt: string }) {
  const response = await send(context, "http://localhost/api/gold-prices", {
    method: "POST",
    headers: authHeaders(token),
    body: JSON.stringify({ items: [item] })
  });
  expect(response.status).toBe(200);
}
