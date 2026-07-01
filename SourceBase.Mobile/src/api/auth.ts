import { api } from "./client";
import { LoginResponse, UserInfo } from "../models";

export interface RegisterBody {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export interface ResetPasswordBody {
  email: string;
  token: string;
  password: string;
}

// NOTE: register / forgot / reset request bodies are named from the Blazor Auth pages.
// Verify exact field names against /swagger before relying on server-side validation.
export const authApi = {
  login: (email: string, password: string) =>
    api.post<LoginResponse>("/auth/login", { email, password }).then((r) => r.data),
  register: (body: RegisterBody) =>
    api.post("/auth/register", body).then((r) => r.data),
  forgotPassword: (email: string) =>
    api.post("/auth/forgot-password", { email }),
  resetPassword: (body: ResetPasswordBody) =>
    api.post("/auth/reset-password", body),
  confirmEmail: (body: { userId: string; token: string }) =>
    api.post("/auth/confirm-email", body),
  info: () => api.get<UserInfo>("/auth/info").then((r) => r.data),
  logout: () => api.post("/auth/logout"),
};
