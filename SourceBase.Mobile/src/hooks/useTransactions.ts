import {
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { transactionsApi, TxnFilters } from "../api/transactions";
import { categoriesApi } from "../api/categories";
import { TransactionType } from "../models";
import { qk } from "../utils/queryKeys";

export function useTransactions(filters: TxnFilters) {
  return useInfiniteQuery({
    queryKey: qk.transactions(filters),
    initialPageParam: 1,
    queryFn: ({ pageParam }) =>
      transactionsApi.list({
        ...filters,
        page: pageParam as number,
        limit: 20,
        order: filters.order ?? "Desc",
        orderBy: filters.orderBy ?? "Date",
      }),
    getNextPageParam: (last) =>
      last.page * last.limit < last.total ? last.page + 1 : undefined,
  });
}

export const useSummary = (p: {
  walletId?: string;
  dateFrom?: string;
  dateTo?: string;
}) =>
  useQuery({
    queryKey: qk.summary(p),
    queryFn: () => transactionsApi.summary(p),
  });

export const useCategories = (type?: TransactionType) =>
  useQuery({
    queryKey: qk.categories(type),
    queryFn: () => categoriesApi.list(type),
  });

export interface UpsertTxnBody {
  walletId: string;
  amount: number;
  type: TransactionType;
  date: string;
  note?: string;
  categoryId: string;
}

export function useUpsertTransaction() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (v: { id?: string; body: UpsertTxnBody }) =>
      v.id ? transactionsApi.update(v.id, v.body) : transactionsApi.create(v.body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["transactions"] });
      qc.invalidateQueries({ queryKey: ["summary"] });
      qc.invalidateQueries({ queryKey: qk.wallets });
      qc.invalidateQueries({ queryKey: qk.stats });
    },
  });
}

export function useDeleteTransaction() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => transactionsApi.remove(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["transactions"] });
      qc.invalidateQueries({ queryKey: ["summary"] });
      qc.invalidateQueries({ queryKey: qk.wallets });
    },
  });
}
