export type TransactionType = "Income" | "Expense";
export type IconGroup = "Wallet" | "Category" | "General";

export type Wallet = { id: string; name: string; balance: number; initialBalance: number; currency: string; icon?: string };
export type WalletsResponse = { wallets: Wallet[]; totalBalance: number };
export type Category = { id: string; userId?: string; name: string; type: TransactionType; icon?: string; isSystem: boolean };
export type Transaction = { id: string; amount: number; type: TransactionType; date: string; note?: string; walletId: string; walletName: string; categoryId?: string; categoryName?: string; isTransfer: boolean };
export type Paged<T> = { items: T[]; page: number; limit: number; total: number };
export type CategoryBreakdown = { categoryId?: string; categoryName?: string; type: TransactionType; total: number };
export type TransactionSummary = { totalIncome: number; totalExpense: number; netBalance: number; byCategory: CategoryBreakdown[] };
export type IconItem = { id: string; value: string; name: string; group: IconGroup; sortOrder: number; isSystem: boolean };
export type ApiError = { traceId?: string; code: string; message: string; errors?: Record<string, string[]> };
export type Toast = { id: string; code: string; message: string; traceId?: string; createdAt: number };

export type TransactionQuery = { page: number; limit: number; walletIds?: string[]; categoryIds?: string[]; type?: TransactionType; dateFrom?: string; dateTo?: string; exclude?: "Transfer" };
export type TransactionInput = { walletId: string; amount: number; type: TransactionType; date: string; note?: string; categoryId: string };
export type TransferInput = { fromWalletId: string; toWalletId: string; amount: number; date: string; note?: string };
export type IconInput = { value: string; name: string; group: IconGroup; sortOrder: number };
