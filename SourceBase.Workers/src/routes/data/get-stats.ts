import type { Hono } from "hono";
import { requireAuth } from "../../lib/auth-middleware";
import type { AppEnv } from "../../types";

export function registerGetStatsRoute(router: Hono<AppEnv>) {
  router.get("/stats", requireAuth, async c => {
    const currentUser = c.get("currentUser");
    const today = getCurrentUtcDate();
    const monthStart = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), 1));
    const nextMonth = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth() + 1, 1));
    const monthPrefix = formatMonthUtc(today);

    const userCount = await c.env.DB.prepare(`SELECT COUNT(*) AS count FROM users`).first<number>("count");
    const totalBalance = await c.env.DB.prepare(
      `SELECT COALESCE(SUM(wallet_total), 0) AS total_balance
       FROM (
         SELECT wallets.id, wallets.initial_balance + COALESCE(SUM(CASE WHEN transactions.type = 'Income' THEN transactions.amount ELSE -transactions.amount END), 0) AS wallet_total
         FROM wallets
         LEFT JOIN transactions ON transactions.wallet_id = wallets.id
         WHERE wallets.user_id = ?
         GROUP BY wallets.id, wallets.initial_balance
       )`
    )
      .bind(currentUser.userId)
      .first<number>("total_balance");
    const monthlyIncome = await c.env.DB.prepare(
      `SELECT COALESCE(SUM(amount), 0) AS amount
       FROM transactions
       WHERE user_id = ? AND type = ? AND date >= ? AND date < ?`
    )
      .bind(currentUser.userId, "Income", formatDateOnlyUtc(monthStart), formatDateOnlyUtc(nextMonth))
      .first<number>("amount");
    const monthlyExpense = await c.env.DB.prepare(
      `SELECT COALESCE(SUM(amount), 0) AS amount
       FROM transactions
       WHERE user_id = ? AND type = ? AND date >= ? AND date < ?`
    )
      .bind(currentUser.userId, "Expense", formatDateOnlyUtc(monthStart), formatDateOnlyUtc(nextMonth))
      .first<number>("amount");
    const loggedDates = await c.env.DB.prepare(
      `SELECT DISTINCT date
       FROM time_sheets
       WHERE user_id = ? AND substr(date, 1, 7) = ? AND hours > 0`
    )
      .bind(currentUser.userId, monthPrefix)
      .all<{ date: string }>();

    const logStatus = computeLogTimeStatus(today, new Set(loggedDates.results.map(result => result.date)));
    return c.json({
      userCount: userCount ?? 0,
      totalBalance: totalBalance ?? 0,
      monthlyIncome: monthlyIncome ?? 0,
      monthlyExpense: monthlyExpense ?? 0,
      allLogged: logStatus.allLogged,
      logTimeDetail: logStatus.logTimeDetail
    });
  });
}

function computeLogTimeStatus(today: Date, loggedDates: Set<string>) {
  const monday = addUtcDays(today, -((today.getUTCDay() - 1 + 7) % 7));
  const weekdaysSoFar: string[] = [];

  for (let date = monday; date <= today; date = addUtcDays(date, 1)) {
    if (date.getUTCDay() === 0 || date.getUTCDay() === 6) continue;
    weekdaysSoFar.push(formatDateOnlyUtc(date));
  }

  if (weekdaysSoFar.length === 0) return { allLogged: false, logTimeDetail: "No weekdays yet this week" };

  const loggedCount = weekdaysSoFar.filter(date => loggedDates.has(date)).length;
  return {
    allLogged: weekdaysSoFar.every(date => loggedDates.has(date)),
    logTimeDetail: `${loggedCount}/${weekdaysSoFar.length} days this week`
  };
}

function getCurrentUtcDate() {
  const now = new Date();
  return new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
}

function addUtcDays(date: Date, days: number) {
  return new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate() + days));
}

function formatDateOnlyUtc(date: Date) {
  const year = date.getUTCFullYear();
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  const day = String(date.getUTCDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatMonthUtc(date: Date) {
  const year = date.getUTCFullYear();
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  return `${year}-${month}`;
}
