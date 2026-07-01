import { api } from "./client";
import { PagingResponse, Todo, TodoItemStatus } from "../models";

export interface TodoFilters {
  status?: TodoItemStatus;
  date?: string;
  todoListId?: string;
  page?: number;
  limit?: number;
  order?: "Asc" | "Desc";
  orderBy?: "Date" | "Title" | "Status" | "CreatedOn" | "CreatedBy";
}

export const todosApi = {
  list: (params: TodoFilters) =>
    api.get<PagingResponse<Todo>>("/todos", { params }).then((r) => r.data),
  create: (b: {
    title: string;
    status: TodoItemStatus;
    date?: string;
    todoListId?: string;
  }) => api.post<{ id: string }>("/todos", b).then((r) => r.data),
  update: (
    id: string,
    b: Partial<{ title: string; date: string; status: TodoItemStatus }>
  ) => api.patch<{ id: string }>(`/todos/${id}`, b).then((r) => r.data),
  remove: (id: string) => api.delete(`/todos/${id}`),
};
