import { api } from "./client";
import { PagingResponse, HabitLog, HabitLogEntry, HabitLogAction } from "../models";

export interface HabitLogFilters {
  action?: HabitLogAction;
  from?: string;
  to?: string;
  page?: number;
  limit?: number;
  order?: "Asc" | "Desc";
  orderBy?: "OccurredAt" | "Action" | "HabitName" | "CreatedOn";
}

export const habitLogsApi = {
  list: (params: HabitLogFilters) =>
    api
      .get<PagingResponse<HabitLog>>("/habit-logs", { params })
      .then((r) => r.data),
  // Batch create: even a single manual log is posted as { entries: [...] }.
  create: (entries: HabitLogEntry[]) =>
    api.post<{ ids: string[] }>("/habit-logs", { entries }).then((r) => r.data),
  remove: (id: string) =>
    api.delete<{ success: boolean }>(`/habit-logs/${id}`).then((r) => r.data),
};
