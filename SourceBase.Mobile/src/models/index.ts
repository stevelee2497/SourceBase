import {
  TransactionType,
  CategoryType,
  TodoItemStatus,
  HabitLogAction,
} from "./enums";

export * from "./enums";

export interface PagingResponse<T> {
  items: T[];
  page: number;
  limit: number;
  total: number;
}

// Auth
export interface LoginResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export interface UserInfo {
  id: string;
  userName?: string;
  email?: string;
  emailConfirmed: boolean;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  avatarUrl?: string;
  defaultTodoListId?: string;
  roles: string[];
}

// Wallets
export interface Wallet {
  id: string;
  name: string;
  balance: number;
  initialBalance: number;
  currency: string;
  icon?: string;
}

export interface GetWalletsResponse {
  wallets: Wallet[];
  totalBalance: number;
}

// Transactions
export interface Transaction {
  id: string;
  amount: number;
  type: TransactionType;
  date: string; // yyyy-MM-dd
  note?: string;
  walletId: string;
  walletName: string;
  categoryId?: string;
  categoryName?: string;
  isTransfer: boolean;
}

export interface CategoryBreakdown {
  categoryId?: string;
  categoryName?: string;
  type: TransactionType;
  total: number;
}

export interface TransactionSummary {
  totalIncome: number;
  totalExpense: number;
  netBalance: number;
  byCategory: CategoryBreakdown[];
}

// Categories
export interface Category {
  id: string;
  userId?: string;
  name: string;
  type: CategoryType;
  icon?: string;
  isSystem: boolean;
}

// Stats
export interface Stats {
  userCount: number;
  totalBalance: number;
  monthlyIncome: number;
  monthlyExpense: number;
  allLogged: boolean;
  logTimeDetail: string;
}

// Habits
export interface Habit {
  id: string;
  name: string;
  icon?: string;
  isSystem: boolean;
  logCount: number;
}

export interface HabitLog {
  id: string;
  habitId?: string;
  habitName?: string;
  action: HabitLogAction;
  occurredAt: string; // ISO DateTime
  createdOn?: string;
}

export interface HabitLogEntry {
  habitId?: string;
  habitName?: string;
  action: HabitLogAction;
  occurredAt: string; // ISO DateTime
}

// Todos
export interface TodoList {
  id: string;
  name: string;
  itemCount: number;
  createdOn?: string;
  createdBy?: string;
  isDefault: boolean;
}

export interface Todo {
  id: string;
  title: string;
  date?: string; // yyyy-MM-dd
  status: TodoItemStatus;
  todoListId?: string;
}

// Errors
export interface ErrorResponse {
  code?: string;
  message?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
