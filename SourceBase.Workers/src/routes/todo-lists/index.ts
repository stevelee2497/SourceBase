import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { registerCreateTodoListRoute } from "./create-todo-list";
import { registerDeleteTodoListRoute } from "./delete-todo-list";
import { registerGetTodoListsRoute } from "./get-todo-lists";
import { registerUpdateTodoListRoute } from "./update-todo-list";

export function buildTodoListsRouter() {
  const router = new Hono<AppEnv>();
  registerCreateTodoListRoute(router);
  registerDeleteTodoListRoute(router);
  registerGetTodoListsRoute(router);
  registerUpdateTodoListRoute(router);
  return router;
}
