import { api } from "./client";
import { Habit } from "../models";

export const habitsApi = {
  list: () => api.get<Habit[]>("/habits").then((r) => r.data),
  create: (b: { name: string; icon?: string }) =>
    api.post<{ id: string }>("/habits", b).then((r) => r.data),
  update: (id: string, b: Partial<{ name: string; icon: string }>) =>
    api.patch<{ id: string }>(`/habits/${id}`, b).then((r) => r.data),
  remove: (id: string) => api.delete(`/habits/${id}`),
};
