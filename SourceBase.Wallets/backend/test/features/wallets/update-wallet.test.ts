import { describe, expect, it } from "vitest";
import { api, createWallet, NIL_ID, systemCategoryId, uniqueUser, unknownId } from "../../helpers";

describe("UpdateWallet", () => {
  it("WALLETS-UPDATE-001: without token returns 401", async () => {
    const res = await api(null, "PATCH", `/wallets/${unknownId()}`, { name: `Updated_${crypto.randomUUID()}`, icon: "🏦" });
    expect(res.status).toBe(401);
  });

  it("WALLETS-UPDATE-002: valid data returns 200", async () => {
    const user = uniqueUser();
    const id = await createWallet(user, { initialBalance: 0, currency: "USD", icon: "💳" });
    const updatedName = `Updated_${crypto.randomUUID()}`;

    const res = await api(user, "PATCH", `/wallets/${id}`, { name: updatedName, icon: "🏦" });

    expect(res.status).toBe(200);
    expect(res.body.id).toBe(id);
    const wallet = await api(user, "GET", `/wallets/${id}`);
    expect(wallet.body.name).toBe(updatedName);
    expect(wallet.body.icon).toBe("🏦");
  });

  it("WALLETS-UPDATE-003: only icon returns 200 and keeps name", async () => {
    const user = uniqueUser();
    const originalName = `Wallet_${crypto.randomUUID()}`;
    const id = await createWallet(user, { name: originalName, initialBalance: 0, currency: "USD", icon: "💳" });

    const res = await api(user, "PATCH", `/wallets/${id}`, { icon: "🏦" });

    expect(res.status).toBe(200);
    const wallet = await api(user, "GET", `/wallets/${id}`);
    expect(wallet.body.name).toBe(originalName);
    expect(wallet.body.icon).toBe("🏦");
  });

  it("WALLETS-UPDATE-004: other user's wallet returns 404", async () => {
    const owner = uniqueUser();
    const other = uniqueUser();
    const id = await createWallet(owner, { initialBalance: 0, currency: "USD", icon: "💳" });

    const res = await api(other, "PATCH", `/wallets/${id}`, { name: `Hacked_${crypto.randomUUID()}`, icon: "🚫" });

    expect(res.status).toBe(404);
  });

  it("WALLETS-UPDATE-005: unknown id returns 404", async () => {
    const res = await api(uniqueUser(), "PATCH", `/wallets/${unknownId()}`, { name: `Unknown_${crypto.randomUUID()}`, icon: "🏦" });
    expect(res.status).toBe(404);
  });

  it("WALLETS-UPDATE-006: computed balance remains unchanged", async () => {
    const user = uniqueUser();
    const id = await createWallet(user, { initialBalance: 100, currency: "USD", icon: "💳" });
    const incomeCategoryId = await systemCategoryId(user, "Income");
    await api(user, "POST", "/transactions", { walletId: id, amount: 25, type: "Income", date: "2025-01-13", note: `Txn_${crypto.randomUUID()}`, categoryId: incomeCategoryId });
    const before = await api(user, "GET", `/wallets/${id}`);

    const update = await api(user, "PATCH", `/wallets/${id}`, { name: `Renamed_${crypto.randomUUID()}`, icon: "🏦" });

    expect(update.status).toBe(200);
    const after = await api(user, "GET", `/wallets/${id}`);
    expect(after.body.balance).toBe(before.body.balance);
    expect(after.body.balance).toBe(125);
  });

  it("WALLETS-UPDATE-007: null icon keeps existing value", async () => {
    const user = uniqueUser();
    const newName = `Wallet_${crypto.randomUUID()}`;
    const id = await createWallet(user, { initialBalance: 0, currency: "USD", icon: "💳" });

    const res = await api(user, "PATCH", `/wallets/${id}`, { name: newName, icon: null });

    expect(res.status).toBe(200);
    const wallet = await api(user, "GET", `/wallets/${id}`);
    expect(wallet.body.name).toBe(newName);
    expect(wallet.body.icon).toBe("💳");
  });

  it("WALLETS-UPDATE-008: empty id returns 400", async () => {
    const res = await api(uniqueUser(), "PATCH", `/wallets/${NIL_ID}`, { name: "Test" });
    expect(res.status).toBe(400);
  });
});
