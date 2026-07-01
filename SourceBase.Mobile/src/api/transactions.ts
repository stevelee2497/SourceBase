import { api } from "./client";
import {
  PagingResponse,
  Transaction,
  TransactionSummary,
  TransactionType,
} from "../models";

export interface TxnFilters {
  walletId?: string;
  type?: TransactionType;
  categoryId?: string;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  limit?: number;
  order?: "Asc" | "Desc";
  orderBy?: "Date" | "Amount" | "Type";
}

export const transactionsApi = {
  list: (params: TxnFilters) =>
    api
      .get<PagingResponse<Transaction>>("/transactions", { params })
      .then((r) => r.data),
  summary: (params: { walletId?: string; dateFrom?: string; dateTo?: string }) =>
    api
      .get<TransactionSummary>("/transactions/summary", { params })
      .then((r) => r.data),
  create: (b: {
    walletId: string;
    amount: number;
    type: TransactionType;
    date: string;
    note?: string;
    categoryId: string;
  }) => api.post<{ id: string }>("/transactions", b).then((r) => r.data),
  update: (
    id: string,
    b: Partial<{
      amount: number;
      type: TransactionType;
      date: string;
      note: string;
      categoryId: string;
    }>
  ) => api.patch<{ id: string }>(`/transactions/${id}`, b).then((r) => r.data),
  remove: (id: string) => api.delete(`/transactions/${id}`),
};
