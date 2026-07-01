import { api } from "./client";
import { Category, CategoryType } from "../models";

export const categoriesApi = {
  list: (type?: CategoryType) =>
    api.get<Category[]>("/categories", { params: { type } }).then((r) => r.data),
};
