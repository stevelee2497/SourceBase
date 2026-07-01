import {
  useInfiniteQuery,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import { habitLogsApi, HabitLogFilters } from "../api/habitLogs";
import { HabitLogEntry } from "../models";
import { qk } from "../utils/queryKeys";

export function useHabitLogs(filters: HabitLogFilters) {
  return useInfiniteQuery({
    queryKey: qk.habitLogs(filters),
    initialPageParam: 1,
    queryFn: ({ pageParam }) =>
      habitLogsApi.list({
        ...filters,
        page: pageParam as number,
        limit: 30,
        order: "Desc",
        orderBy: "OccurredAt",
      }),
    getNextPageParam: (last) =>
      last.page * last.limit < last.total ? last.page + 1 : undefined,
  });
}

export function useCreateHabitLog() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (entries: HabitLogEntry[]) => habitLogsApi.create(entries),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["habitLogs"] });
      qc.invalidateQueries({ queryKey: qk.habits });
    },
  });
}

export function useDeleteHabitLog() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => habitLogsApi.remove(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["habitLogs"] });
      qc.invalidateQueries({ queryKey: qk.habits });
    },
  });
}
