import { api } from "./client";
import { UserInfo } from "../models";

export const profileApi = {
  get: () => api.get<UserInfo>("/auth/info").then((r) => r.data),
  update: (
    b: Partial<{
      firstName: string;
      lastName: string;
      phoneNumber: string;
      avatarUrl: string;
      defaultTodoListId: string;
    }>
  ) => api.patch<{ id: string }>("/auth/info", b).then((r) => r.data),
};
