export const qk = {
  stats: ["stats"] as const,
  wallets: ["wallets"] as const,
  wallet: (id: string) => ["wallet", id] as const,
  transactions: (filters: object) => ["transactions", filters] as const,
  summary: (filters: object) => ["summary", filters] as const,
  categories: (type?: string) => ["categories", type ?? "all"] as const,
  habits: ["habits"] as const,
  habitLogs: (filters: object) => ["habitLogs", filters] as const,
  todoLists: ["todoLists"] as const,
  todos: (filters: object) => ["todos", filters] as const,
  me: ["me"] as const,
};
