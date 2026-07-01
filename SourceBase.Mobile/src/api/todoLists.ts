import { api } from "./client";
import { PagingResponse, TodoList } from "../models";

export const todoListsApi = {
  list: () =>
    api
      .get<PagingResponse<TodoList>>("/todo-lists", {
        params: { page: 1, limit: 50, order: "Desc", orderBy: "CreatedOn" },
      })
      .then((r) => r.data),
  create: (name: string) =>
    api.post<{ id: string }>("/todo-lists", { name }).then((r) => r.data),
};
