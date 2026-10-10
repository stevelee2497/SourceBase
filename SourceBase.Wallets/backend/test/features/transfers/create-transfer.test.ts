import { describe, expect, it } from "vitest";
import { api, createTransfer, createWallet, getWallet, unknownId, uniqueUser } from "../../helpers";

describe("CreateTransfer", () => {
  it("TRANSFER-CREATE-001: no token returns 401", async () => {
    const res = await api(null, "POST", "/transfers", { fromWalletId: unknownId(), toWalletId: unknownId(), amount: 10, date: "2025-04-01" });
    expect(res.status).toBe(401);
  });

  it("TRANSFER-CREATE-002: valid data updates both wallet balances", async () => {
    const user = uniqueUser();
    const fromId = await createWallet(user, { initialBalance: 100 });
    const toId = await createWallet(user, { initialBalance: 50 });
    const res = await api(user, "POST", "/transfers", { fromWalletId: fromId, toWalletId: toId, amount: 30, date: "2025-04-02", note: `Transfer_${crypto.randomUUID()}` });
    expect(res.status).toBe(200);
    expect(res.body.id).toBeTruthy();
    expect((await getWallet(user, fromId)).balance).toBe(70);
    expect((await getWallet(user, toId)).balance).toBe(80);
  });

  it("TRANSFER-CREATE-003: same wallets returns 400", async () => {
    const user = uniqueUser();
    const walletId = await createWallet(user, { initialBalance: 100 });
    const res = await api(user, "POST", "/transfers", { fromWalletId: walletId, toWalletId: walletId, amount: 10, date: "2025-04-03" });
    expect(res.status).toBe(400);
  });

  it("TRANSFER-CREATE-004: other user's from wallet returns 400", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const otherWalletId = await createWallet(other, { initialBalance: 100 });
    const ownWalletId = await createWallet(owner, { initialBalance: 50 });
    const res = await api(owner, "POST", "/transfers", { fromWalletId: otherWalletId, toWalletId: ownWalletId, amount: 10, date: "2025-04-04" });
    expect(res.status).toBe(400);
  });

  it("TRANSFER-CREATE-005: other user's to wallet returns 400", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const ownWalletId = await createWallet(owner, { initialBalance: 100 });
    const otherWalletId = await createWallet(other, { initialBalance: 50 });
    const res = await api(owner, "POST", "/transfers", { fromWalletId: ownWalletId, toWalletId: otherWalletId, amount: 10, date: "2025-04-05" });
    expect(res.status).toBe(400);
  });

  it("TRANSFER-CREATE-006: zero or negative amount returns 400", async () => {
    const user = uniqueUser();
    const fromId = await createWallet(user, { initialBalance: 100 });
    const toId = await createWallet(user, { initialBalance: 50 });
    const zero = await api(user, "POST", "/transfers", { fromWalletId: fromId, toWalletId: toId, amount: 0, date: "2025-04-06" });
    const negative = await api(user, "POST", "/transfers", { fromWalletId: fromId, toWalletId: toId, amount: -1, date: "2025-04-06" });
    expect(zero.status).toBe(400);
    expect(negative.status).toBe(400);
  });

  it("TRANSFER-CREATE-007: missing date returns 400", async () => {
    const user = uniqueUser();
    const fromId = await createWallet(user, { initialBalance: 100 });
    const toId = await createWallet(user, { initialBalance: 50 });
    const res = await api(user, "POST", "/transfers", { fromWalletId: fromId, toWalletId: toId, amount: 10 });
    expect(res.status).toBe(400);
  });

  it("TRANSFER-CREATE-008: creates linked transfer transactions", async () => {
    const user = uniqueUser();
    const fromId = await createWallet(user, { initialBalance: 100 });
    const toId = await createWallet(user, { initialBalance: 50 });
    const res = await api(user, "POST", "/transfers", { fromWalletId: fromId, toWalletId: toId, amount: 35, date: "2025-04-07", note: `Transfer_${crypto.randomUUID()}` });
    expect(res.status).toBe(200);
    const fromTxns = await api(user, "GET", `/transactions?walletIds=${fromId}&limit=100`);
    expect(fromTxns.body.items.some((x: any) => x.type === "Expense" && x.isTransfer)).toBe(true);
    const toTxns = await api(user, "GET", `/transactions?walletIds=${toId}&limit=100`);
    expect(toTxns.body.items.some((x: any) => x.type === "Income" && x.isTransfer)).toBe(true);
  });

  it("TRANSFER-CREATE-009: missing from wallet returns 400", async () => {
    const user = uniqueUser();
    const toId = await createWallet(user, { initialBalance: 50 });
    const res = await api(user, "POST", "/transfers", { toWalletId: toId, amount: 10, date: "2025-04-08" });
    expect(res.status).toBe(400);
  });

  it("TRANSFER-CREATE-010: unknown from wallet returns 400", async () => {
    const user = uniqueUser();
    const toId = await createWallet(user, { initialBalance: 50 });
    const res = await api(user, "POST", "/transfers", { fromWalletId: unknownId(), toWalletId: toId, amount: 10, date: "2025-04-09" });
    expect(res.status).toBe(400);
  });

  it("TRANSFER-CREATE-011: unknown to wallet returns 400", async () => {
    const user = uniqueUser();
    const fromId = await createWallet(user, { initialBalance: 100 });
    const res = await api(user, "POST", "/transfers", { fromWalletId: fromId, toWalletId: unknownId(), amount: 10, date: "2025-04-10" });
    expect(res.status).toBe(400);
  });

  it("NEW (not in .NET): deleting a wallet removes its transfers and the other wallet's leg", async () => {
    const user = uniqueUser();
    const fromId = await createWallet(user, { initialBalance: 100 });
    const toId = await createWallet(user, { initialBalance: 50 });
    const transferId = await createTransfer(user, fromId, toId, { amount: 30, date: "2025-04-11" });
    expect((await getWallet(user, toId)).balance).toBe(80);
    const del = await api(user, "DELETE", `/wallets/${fromId}`);
    expect(del.status).toBe(200);
    expect((await getWallet(user, toId)).balance).toBe(50);
    const list = await api(user, "GET", "/transfers?limit=100");
    expect(list.body.items.some((x: any) => x.id === transferId)).toBe(false);
    expect(list.body.total).toBe(0);
    const toTxns = await api(user, "GET", `/transactions?walletIds=${toId}&limit=100`);
    expect(toTxns.body.items.some((x: any) => x.isTransfer)).toBe(false);
  });
});
