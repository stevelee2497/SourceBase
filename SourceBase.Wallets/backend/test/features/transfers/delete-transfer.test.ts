import { describe, expect, it } from "vitest";
import { api, createTransfer, createWallet, getWallet, unknownId, uniqueUser } from "../../helpers";

describe("DeleteTransfer", () => {
  it("TRANSFER-DELETE-001: without token return 401", async () => {
    const res = await api(null, "DELETE", `/transfers/${unknownId()}`);
    expect(res.status).toBe(401);
  });

  it("TRANSFER-DELETE-002: owned transfer removes linked transactions and restores balances", async () => {
    const user = uniqueUser();
    const fromId = await createWallet(user, { initialBalance: 100 });
    const toId = await createWallet(user, { initialBalance: 50 });
    const transferId = await createTransfer(user, fromId, toId, { amount: 30, date: "2025-04-16", note: `Transfer_${crypto.randomUUID()}` });
    const res = await api(user, "DELETE", `/transfers/${transferId}`);
    expect(res.status).toBe(200);
    expect(res.body.success).toBe(true);
    expect((await getWallet(user, fromId)).balance).toBe(100);
    expect((await getWallet(user, toId)).balance).toBe(50);
  });

  it("TRANSFER-DELETE-003: unknown id return 404", async () => {
    const res = await api(uniqueUser(), "DELETE", `/transfers/${unknownId()}`);
    expect(res.status).toBe(404);
  });

  it("TRANSFER-DELETE-004: other user's transfer return 404", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const fromId = await createWallet(owner, { initialBalance: 100 });
    const toId = await createWallet(owner, { initialBalance: 50 });
    const transferId = await createTransfer(owner, fromId, toId, { amount: 10, date: "2025-04-17", note: `Transfer_${crypto.randomUUID()}` });
    const res = await api(other, "DELETE", `/transfers/${transferId}`);
    expect(res.status).toBe(404);
  });

  it("TRANSFER-DELETE-005: removes both linked transactions", async () => {
    const user = uniqueUser();
    const fromId = await createWallet(user, { initialBalance: 100 });
    const toId = await createWallet(user, { initialBalance: 50 });
    const transferId = await createTransfer(user, fromId, toId, { amount: 25, date: "2025-04-18", note: `Transfer_${crypto.randomUUID()}` });
    const res = await api(user, "DELETE", `/transfers/${transferId}`);
    expect(res.status).toBe(200);
    const list = await api(user, "GET", "/transfers?limit=100");
    expect(list.body.items.some((x: any) => x.id === transferId)).toBe(false);
    const fromTxns = await api(user, "GET", `/transactions?walletIds=${fromId}&limit=100`);
    expect(fromTxns.body.items.some((x: any) => x.isTransfer)).toBe(false);
    const toTxns = await api(user, "GET", `/transactions?walletIds=${toId}&limit=100`);
    expect(toTxns.body.items.some((x: any) => x.isTransfer)).toBe(false);
  });

  it("TRANSFER-DELETE-006: does not affect other transfers", async () => {
    const user = uniqueUser();
    const fromId = await createWallet(user, { initialBalance: 100 });
    const toId = await createWallet(user, { initialBalance: 50 });
    const transfer1 = await createTransfer(user, fromId, toId, { amount: 20, date: "2025-04-19", note: `Transfer_${crypto.randomUUID()}` });
    const transfer2 = await createTransfer(user, fromId, toId, { amount: 10, date: "2025-04-20", note: `Transfer_${crypto.randomUUID()}` });
    const res = await api(user, "DELETE", `/transfers/${transfer1}`);
    expect(res.status).toBe(200);
    const list = await api(user, "GET", "/transfers?limit=20");
    expect(list.body.items.some((x: any) => x.id === transfer2)).toBe(true);
    expect(list.body.items.some((x: any) => x.id === transfer1)).toBe(false);
  });
});
