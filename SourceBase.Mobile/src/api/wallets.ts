import { api } from "./client";
import { GetWalletsResponse, Wallet } from "../models";

export const walletsApi = {
  list: () => api.get<GetWalletsResponse>("/wallets").then((r) => r.data),
  get: (id: string) => api.get<Wallet>(`/wallets/${id}`).then((r) => r.data),
  create: (b: { name: string; currency: string; initialBalance: number; icon?: string }) =>
    api.post<{ id: string }>("/wallets", b).then((r) => r.data),
};
