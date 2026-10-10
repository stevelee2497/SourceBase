import { useAuth0 } from "@auth0/auth0-react";
import { useMemo } from "react";
import { showError } from "./toasts";
import type { ApiError, Category, IconInput, IconItem, Paged, Transaction, TransactionInput, TransactionQuery, TransactionSummary, TransferInput, Wallet, WalletsResponse } from "./types";

const API_URL = import.meta.env.VITE_API_URL ?? "";
const AUDIENCE = import.meta.env.VITE_AUTH0_AUDIENCE;

export type ApiResult<T> = { data?: T; error?: ApiError };
type Options = { silent?: boolean };

const query = (params: Record<string, string | number | string[] | undefined>) => {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === "") continue;
    for (const v of Array.isArray(value) ? value : [value]) search.append(key, String(v));
  }
  const text = search.toString();
  return text ? `?${text}` : "";
};

/** Typed API client. Every call attaches the Auth0 token, and failed calls raise a toast unless `silent`. */
export const useApi = () => {
  const { getAccessTokenSilently } = useAuth0();

  return useMemo(() => {
    const request = async <T>(method: string, path: string, body?: unknown, options: Options = {}): Promise<ApiResult<T>> => {
      const token = await getAccessTokenSilently({ authorizationParams: { audience: AUDIENCE } });
      const isForm = body instanceof FormData;
      const headers: Record<string, string> = { Authorization: `Bearer ${token}` };
      if (body !== undefined && !isForm) headers["Content-Type"] = "application/json";
      const res = await fetch(`${API_URL}/api${path}`, { method, headers, body: body === undefined ? undefined : isForm ? body : JSON.stringify(body) });
      const text = await res.text();
      const json = text ? JSON.parse(text) : undefined;
      if (res.ok) return { data: json as T };
      const error = json as ApiError;
      if (!options.silent) showError(error);
      return { error };
    };

    return {
      getWallets: () => request<WalletsResponse>("GET", "/wallets"),
      createWallet: (body: { name: string; initialBalance: number; currency: string; icon?: string }) => request<{ id: string }>("POST", "/wallets", body),
      updateWallet: (id: string, body: { name?: string; icon?: string }) => request<{ id: string }>("PATCH", `/wallets/${id}`, body),
      configureWallet: (id: string, currency: string) => request<{ id: string }>("PUT", `/wallets/${id}/config`, { currency }),
      deleteWallet: (id: string) => request<{ success: boolean }>("DELETE", `/wallets/${id}`),

      getCategories: () => request<Category[]>("GET", "/categories"),
      createCategory: (body: { name: string; type: string; icon?: string }) => request<{ id: string }>("POST", "/categories", body),
      updateCategory: (id: string, body: { name?: string; icon?: string }) => request<{ id: string }>("PATCH", `/categories/${id}`, body),
      deleteCategory: (id: string) => request<{ success: boolean }>("DELETE", `/categories/${id}`),

      getTransactions: (q: TransactionQuery) => request<Paged<Transaction>>("GET", `/transactions${query({ ...q })}`),
      getSummary: (q: { walletIds?: string[]; dateFrom?: string; dateTo?: string }) => request<TransactionSummary>("GET", `/transactions/summary${query({ ...q })}`),
      createTransaction: (body: TransactionInput) => request<{ id: string }>("POST", "/transactions", body),
      updateTransaction: (id: string, body: Partial<TransactionInput>) => request<{ id: string }>("PATCH", `/transactions/${id}`, body),
      deleteTransaction: (id: string) => request<{ success: boolean }>("DELETE", `/transactions/${id}`),
      createTransfer: (body: TransferInput) => request<{ id: string }>("POST", "/transfers", body),

      getIcons: (group?: string, options?: Options) => request<IconItem[]>("GET", `/icons${query({ group })}`, undefined, options),
      createIcon: (body: IconInput) => request<{ id: string }>("POST", "/icons", body),
      updateIcon: (id: string, body: Partial<IconInput>) => request<{ id: string }>("PATCH", `/icons/${id}`, body),
      deleteIcon: (id: string) => request<{ success: boolean }>("DELETE", `/icons/${id}`),
      uploadIcon: (file: File) => { const form = new FormData(); form.append("file", file); return request<{ iconUrl: string }>("POST", "/icons/upload", form); },
    };
  }, [getAccessTokenSilently]);
};

export type Api = ReturnType<typeof useApi>;
export type { Wallet };
