import {
  useQuery,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import { todoListsApi } from "../api/todoLists";
import { todosApi, TodoFilters } from "../api/todos";
import { PagingResponse, Todo, TodoItemStatus } from "../models";
import { qk } from "../utils/queryKeys";

export const useTodoLists = () =>
  useQuery({ queryKey: qk.todoLists, queryFn: todoListsApi.list });

export const useTodos = (filters: TodoFilters) =>
  useQuery({
    queryKey: qk.todos(filters),
    queryFn: () =>
      todosApi.list({ ...filters, limit: 200, orderBy: "Date", order: "Asc" }),
    enabled: !!filters.todoListId,
  });

export function useCreateTodo(filters: TodoFilters) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (b: { title: string; date?: string }) =>
      todosApi.create({
        title: b.title,
        date: b.date,
        status: "Open",
        todoListId: filters.todoListId,
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["todos"] }),
  });
}

// Optimistic toggle — the defining Microsoft To Do interaction.
export function useToggleTodo(filters: TodoFilters) {
  const qc = useQueryClient();
  const key = qk.todos({ ...filters, limit: 200, orderBy: "Date", order: "Asc" });
  return useMutation({
    mutationFn: (t: Todo) =>
      todosApi.update(t.id, {
        status: t.status === "Completed" ? "Open" : "Completed",
      }),
    onMutate: async (t) => {
      await qc.cancelQueries({ queryKey: key });
      const prev = qc.getQueryData<PagingResponse<Todo>>(key);
      qc.setQueryData<PagingResponse<Todo>>(key, (old) =>
        old
          ? {
              ...old,
              items: old.items.map((i) =>
                i.id === t.id
                  ? {
                      ...i,
                      status: i.status === "Completed" ? "Open" : "Completed",
                    }
                  : i
              ),
            }
          : old
      );
      return { prev };
    },
    onError: (_e, _t, ctx) => {
      if (ctx?.prev) qc.setQueryData(key, ctx.prev);
    },
    onSettled: () => qc.invalidateQueries({ queryKey: ["todos"] }),
  });
}

export function useUpdateTodo() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (v: {
      id: string;
      body: Partial<{ title: string; date: string; status: TodoItemStatus }>;
    }) => todosApi.update(v.id, v.body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["todos"] }),
  });
}

export function useDeleteTodo() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => todosApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["todos"] }),
  });
}
