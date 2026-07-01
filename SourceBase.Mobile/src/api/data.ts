import { api } from "./client";
import { Stats } from "../models";

export const dataApi = {
  stats: () => api.get<Stats>("/data/stats").then((r) => r.data),
};
