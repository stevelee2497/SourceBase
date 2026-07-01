import { useQuery } from "@tanstack/react-query";
import { dataApi } from "../api/data";
import { transactionsApi } from "../api/transactions";
import { todosApi } from "../api/todos";
import { toApiDate } from "../utils/date";
import { qk } from "../utils/queryKeys";

export const useStats = () =>
  useQuery({ queryKey: qk.stats, queryFn: dataApi.stats });

export const useRecentTransactions = () =>
  useQuery({
    queryKey: qk.transactions({ recent: true }),
    queryFn: () =>
      transactionsApi.list({ limit: 5, order: "Desc", orderBy: "Date" }),
  });

export const useTodayOpenCount = () =>
  useQuery({
    queryKey: qk.todos({ today: true }),
    queryFn: () =>
      todosApi.list({ status: "Open", date: toApiDate(new Date()), limit: 1 }),
    select: (r) => r.total,
  });
