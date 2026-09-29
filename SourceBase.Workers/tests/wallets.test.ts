import { beforeEach, describe, expect, it } from "vitest";
import { createTestContext, createUserAndGetAccessToken, createWallet, getAdminAccessToken, send, type TestContext } from "./helpers";

describe("SourceBase.Workers wallet routes", () => {
  let context: TestContext;
  let token: string;

  beforeEach(async () => {
    context = await createTestContext();
    token = await getAdminAccessToken(context);
  });

  it("creates, lists, gets, updates, configures, and deletes wallets", async () => {
    const createResult = await createWallet(context, token, {
      name: "Main Wallet",
      initialBalance: 125.5,
      currency: "usd",
      icon: "💳"
    });
    expect(createResult.response.status).toBe(200);

    const listResponse = await send(context, "http://localhost/api/wallets", {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(listResponse.status).toBe(200);
    const listBody = await listResponse.json() as {
      wallets: Array<{ id: string; name: string; balance: number; initialBalance: number; currency: string; icon: string | null }>;
      totalBalance: number;
    };
    expect(listBody.totalBalance).toBe(125.5);
    expect(listBody.wallets).toContainEqual({
      id: createResult.body.id,
      name: "Main Wallet",
      balance: 125.5,
      initialBalance: 125.5,
      currency: "usd",
      icon: "💳"
    });

    const getResponse = await send(context, `http://localhost/api/wallets/${createResult.body.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(getResponse.status).toBe(200);

    const patchResponse = await send(context, `http://localhost/api/wallets/${createResult.body.id}`, {
      method: "PATCH",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({ name: "Travel Wallet", icon: null })
    });
    expect(patchResponse.status).toBe(200);

    const configResponse = await send(context, `http://localhost/api/wallets/${createResult.body.id}/config`, {
      method: "PUT",
      headers: { Authorization: `Bearer ${token}` },
      body: JSON.stringify({ currency: "  eur  " })
    });
    expect(configResponse.status).toBe(200);

    const afterConfigResponse = await send(context, `http://localhost/api/wallets/${createResult.body.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const afterConfigBody = await afterConfigResponse.json() as {
      name: string;
      balance: number;
      initialBalance: number;
      currency: string;
      icon: string | null;
    };
    expect(afterConfigBody).toMatchObject({
      name: "Travel Wallet",
      balance: 125.5,
      initialBalance: 125.5,
      currency: "EUR",
      icon: "💳"
    });

    const secondUserToken = await createUserAndGetAccessToken(context);
    const secondUserGetResponse = await send(context, `http://localhost/api/wallets/${createResult.body.id}`, {
      headers: { Authorization: `Bearer ${secondUserToken}` }
    });
    expect(secondUserGetResponse.status).toBe(404);

    const deleteResponse = await send(context, `http://localhost/api/wallets/${createResult.body.id}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(deleteResponse.status).toBe(200);
    expect(await deleteResponse.json()).toEqual({ success: true });
  });
});
