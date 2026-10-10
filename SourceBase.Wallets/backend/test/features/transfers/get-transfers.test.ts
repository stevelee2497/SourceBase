import { describe, expect, it } from "vitest";
import { api, createTransfer, createWallet, uniqueUser } from "../../helpers";

describe("GetTransfers", () => {
  it("TRANSFER-GET-001: missing token returns 401", async () => {
    const res = await api(null, "GET", "/transfers");
    expect(res.status).toBe(401);
  });

  it("TRANSFER-GET-002: owned transfers return 200", async () => {
    const user = uniqueUser();
    const fromId = await createWallet(user, { initialBalance: 100 });
    const toId = await createWallet(user, { initialBalance: 50 });
    const transferId = await createTransfer(user, fromId, toId, { amount: 15, date: "2025-04-11", note: `Transfer_${crypto.randomUUID()}` });
    const res = await api(user, "GET", "/transfers?limit=20");
    expect(res.status).toBe(200);
    expect(res.body.items.some((x: any) => x.id === transferId)).toBe(true);
    expect(res.body.total).toBe(1);
  });

  it("TRANSFER-GET-003: returns complete transfer details", async () => {
    const user = uniqueUser();
    const fromId = await createWallet(user, { initialBalance: 100 });
    const toId = await createWallet(user, { initialBalance: 50 });
    const note = `Transfer_${crypto.randomUUID()}`;
    const transferId = await createTransfer(user, fromId, toId, { amount: 20, date: "2025-04-12", note });
    const res = await api(user, "GET", "/transfers?limit=20");
    expect(res.status).toBe(200);
    const items = res.body.items.filter((x: any) => x.id === transferId);
    expect(items).toHaveLength(1);
    expect(items[0].fromWalletId).toBe(fromId);
    expect(items[0].toWalletId).toBe(toId);
    expect(items[0].amount).toBe(20);
    expect(items[0].date).toBe("2025-04-12");
    expect(items[0].note).toBe(note);
  });

  it("TRANSFER-GET-004: multiple users returns only user's transfers", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const ownerFrom = await createWallet(owner, { initialBalance: 100 });
    const ownerTo = await createWallet(owner, { initialBalance: 50 });
    const otherFrom = await createWallet(other, { initialBalance: 100 });
    const otherTo = await createWallet(other, { initialBalance: 50 });
    const ownTransfer = await createTransfer(owner, ownerFrom, ownerTo, { amount: 15, date: "2025-04-13", note: `Transfer_${crypto.randomUUID()}` });
    const otherTransfer = await createTransfer(other, otherFrom, otherTo, { amount: 20, date: "2025-04-13", note: `Transfer_${crypto.randomUUID()}` });
    const res = await api(owner, "GET", "/transfers?limit=20");
    expect(res.status).toBe(200);
    expect(res.body.items.some((x: any) => x.id === ownTransfer)).toBe(true);
    expect(res.body.items.some((x: any) => x.id === otherTransfer)).toBe(false);
  });

  it("TRANSFER-GET-005: wallet filter returns matching transfers", async () => {
    const user = uniqueUser();
    const walletA = await createWallet(user, { initialBalance: 100 });
    const walletB = await createWallet(user, { initialBalance: 100 });
    const walletC = await createWallet(user, { initialBalance: 100 });
    const matching = await createTransfer(user, walletA, walletB, { amount: 10, date: "2025-04-14", note: `Transfer_${crypto.randomUUID()}` });
    const nonMatching = await createTransfer(user, walletB, walletC, { amount: 12, date: "2025-04-15", note: `Transfer_${crypto.randomUUID()}` });
    const res = await api(user, "GET", `/transfers?walletId=${walletA}&limit=20`);
    expect(res.status).toBe(200);
    expect(res.body.items.some((x: any) => x.id === matching)).toBe(true);
    expect(res.body.items.some((x: any) => x.id === nonMatching)).toBe(false);
  });

  it("TRANSFER-GET-006: date range returns transfers within range", async () => {
    const user = uniqueUser();
    const fromId = await createWallet(user, { initialBalance: 100 });
    const toId = await createWallet(user, { initialBalance: 50 });
    const before = await createTransfer(user, fromId, toId, { amount: 10, date: "2025-04-01", note: `Transfer_${crypto.randomUUID()}` });
    const inRange = await createTransfer(user, fromId, toId, { amount: 15, date: "2025-04-15", note: `Transfer_${crypto.randomUUID()}` });
    const after = await createTransfer(user, fromId, toId, { amount: 20, date: "2025-05-01", note: `Transfer_${crypto.randomUUID()}` });
    const res = await api(user, "GET", "/transfers?dateFrom=2025-04-10&dateTo=2025-04-20&limit=20");
    expect(res.status).toBe(200);
    expect(res.body.items.some((x: any) => x.id === inRange)).toBe(true);
    expect(res.body.items.some((x: any) => x.id === before || x.id === after)).toBe(false);
  });

  it("TRANSFER-GET-007: pagination returns correct subset", async () => {
    const user = uniqueUser();
    const fromId = await createWallet(user, { initialBalance: 100 });
    const toId = await createWallet(user, { initialBalance: 50 });
    await createTransfer(user, fromId, toId, { amount: 10, date: "2025-04-01", note: `Transfer_${crypto.randomUUID()}` });
    const second = await createTransfer(user, fromId, toId, { amount: 20, date: "2025-04-02", note: `Transfer_${crypto.randomUUID()}` });
    await createTransfer(user, fromId, toId, { amount: 30, date: "2025-04-03", note: `Transfer_${crypto.randomUUID()}` });
    const res = await api(user, "GET", "/transfers?page=2&limit=1");
    expect(res.status).toBe(200);
    expect(res.body.total).toBe(3);
    expect(res.body.items).toHaveLength(1);
    expect(res.body.items[0].id).toBe(second);
  });

  it("TRANSFER-GET-008: no transfers returns empty list", async () => {
    const res = await api(uniqueUser(), "GET", "/transfers?limit=20");
    expect(res.status).toBe(200);
    expect(res.body.items).toHaveLength(0);
    expect(res.body.total).toBe(0);
  });
});
