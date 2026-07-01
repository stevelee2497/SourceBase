import { useQuery } from "@tanstack/react-query";
import { habitsApi } from "../api/habits";
import { qk } from "../utils/queryKeys";

export const useHabits = () =>
  useQuery({ queryKey: qk.habits, queryFn: habitsApi.list });
