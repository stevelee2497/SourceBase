import { Hono } from "hono";
import type { AppEnv } from "../../types";
import { registerCreateTodoRoute } from "./create-todo";
import { registerDeleteTodoRoute } from "./delete-todo";
import { registerGetTodoRoute } from "./get-todo";
import { registerGetTodosRoute } from "./get-todos";
import { registerUpdateTodoRoute } from "./update-todo";

export function buildTodosRouter() {
  const router = new Hono<AppEnv>();
  registerCreateTodoRoute(router);
  registerDeleteTodoRoute(router);
  registerGetTodoRoute(router);
  registerGetTodosRoute(router);
  registerUpdateTodoRoute(router);
  return router;
}
