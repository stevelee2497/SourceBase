import { useCallback, useEffect, useMemo, useState } from "react";
import { ConfirmDialog } from "../../components/ConfirmDialog";
import { Icon } from "../../components/Icon";
import { MultiSelectInput } from "../../components/MultiSelectInput";
import { Pagination } from "../../components/Pagination";
import { Spinner } from "../../components/Spinner";
import { useApi } from "../../lib/api";
import { DATE_RANGE, DATE_RANGE_OPTIONS, mergeDateRanges, resolveDateRange, type Custom } from "../../lib/dateRanges";
import { formatAmount, formatDisplayDate, signedAmount } from "../../lib/format";
import type { Category, Transaction, TransactionType, Wallet } from "../../lib/types";
import { PAGE_SIZE, TRANSACTION_TYPE } from "./constants";
import { CustomRangeModal } from "./CustomRangeModal";
import { PieChart } from "./PieChart";
import { TransactionModal } from "./TransactionModal";
import { TransferModal } from "./TransferModal";
import { WalletModal } from "./WalletModal";

const DEFAULT_RANGE = [DATE_RANGE.thisYear];
const NO_CUSTOM: Custom = { from: null, to: null };
const ERROR_BOX = "mb-4 rounded-md bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 px-4 py-3 text-sm text-red-700 dark:text-red-400";
const TILE_BASE = "relative rounded-lg border-2 cursor-pointer transition-all p-2 md:p-3 flex flex-col gap-1 md:gap-1.5 justify-between";
const ICON_PATHS = {
  edit: "M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z",
  trash: "M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16",
  plus: "M12 4v16m8-8H4",
  transfer: "M7 16V4m0 0L3 8m4-4l4 4m6 0v12m0 0l-4-4m4 4l4-4",
};

const SvgIcon = ({ path, className }: { path: string; className: string }) => (
  <svg className={className} fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d={path} /></svg>
);

const isIncome = (type: string) => type.toLowerCase() === TRANSACTION_TYPE.income.toLowerCase();
const amountClass = (type: string) => (isIncome(type) ? "text-green-600 dark:text-green-400" : "text-red-600 dark:text-red-400");
const txFallbackIcon = (t: Transaction) => (t.isTransfer ? "🔁" : isIncome(t.type) ? "💰" : "💸");
const txTitle = (t: Transaction) => t.note?.trim() || "No description";
const txCategoryLabel = (t: Transaction) => (t.isTransfer ? "Transfer" : t.categoryName?.trim() || "Uncategorized");

type WalletTileProps = { wallet: Wallet; selected: boolean; onSelect: () => void; onEdit: () => void; onDelete: () => void };

const WalletTile = ({ wallet, selected, onSelect, onEdit, onDelete }: WalletTileProps) => {
  const stop = (action: () => void) => (e: React.MouseEvent) => { e.stopPropagation(); action(); };
  const state = selected ? "border-indigo-500 bg-indigo-50 dark:bg-indigo-900/30" : "border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 hover:border-indigo-300 hover:bg-gray-50 dark:hover:bg-gray-700";
  return (
    <div className={`${TILE_BASE} ${state}`} onClick={onSelect}>
      <div className="absolute top-1.5 right-1.5 flex gap-1">
        <button className="text-gray-400 dark:text-gray-500 hover:text-indigo-600 dark:hover:text-indigo-400" onClick={stop(onEdit)} title="Edit"><SvgIcon className="w-3.5 h-3.5" path={ICON_PATHS.edit} /></button>
        <button className="text-gray-400 dark:text-gray-500 hover:text-red-600 dark:hover:text-red-400" onClick={stop(onDelete)} title="Delete"><SvgIcon className="w-3.5 h-3.5" path={ICON_PATHS.trash} /></button>
      </div>
      <div className="flex items-center justify-start gap-1">
        <span className="text-xl leading-none"><Icon value={wallet.icon} fallback="👛" size="w-6 h-6" emojiClass="" /></span>
      </div>
      <div className={`text-xs font-semibold ${selected ? "text-indigo-700 dark:text-indigo-400" : "text-gray-700 dark:text-gray-200"} truncate`}>{wallet.name}</div>
      <div className={`text-sm font-bold ${selected ? "text-indigo-700 dark:text-indigo-400" : "text-gray-900 dark:text-gray-100"}`}>{formatAmount(wallet.balance)}</div>
    </div>
  );
};

type RowProps = { transaction: Transaction; category?: Category; onClick: () => void };

const TransactionRow = ({ transaction: t, category, onClick }: RowProps) => (
  <div className={`py-2 flex flex-col sm:flex-row sm:items-center gap-2 ${t.isTransfer ? "" : "cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700"}`} onClick={t.isTransfer ? undefined : onClick}>
    <div className="flex items-center gap-3 flex-1 min-w-0">
      <div className="w-10 h-10 rounded-full bg-gray-100 dark:bg-gray-700/50 flex items-center justify-center text-lg shrink-0">
        <Icon value={category?.icon} fallback={txFallbackIcon(t)} size="w-6 h-6" emojiClass="" />
      </div>
      <div className="min-w-0 flex-1">
        <div className="font-medium text-gray-900 dark:text-gray-100 truncate">{txCategoryLabel(t)}</div>
        <div className="text-sm text-gray-500 dark:text-gray-400 truncate flex">
          {txTitle(t)}
          <div className="ml-1 flex flex-wrap items-center gap-1 text-xs italic text-gray-400 dark:text-gray-500">
            <span>•</span>
            <span>{formatDisplayDate(t.date)}</span>
            {t.isTransfer && <span className="badge-blue hidden lg:block">Transfer</span>}
          </div>
        </div>
      </div>
      <div className={`font-semibold ${amountClass(t.type)}`}>{signedAmount(t.amount, t.type)}</div>
    </div>
  </div>
);

export const WalletsPage = () => {
  const api = useApi();
  const [wallets, setWallets] = useState<Wallet[]>([]);
  const [totalBalance, setTotalBalance] = useState(0);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<string[]>([]);

  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loadingTransactions, setLoadingTransactions] = useState(false);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [refreshKey, setRefreshKey] = useState(0);
  const [summary, setSummary] = useState({ totalIncome: 0, totalExpense: 0 });

  const [typeFilters, setTypeFilters] = useState<TransactionType[]>([]);
  const [categoryIds, setCategoryIds] = useState<string[]>([]);
  const [dateFilters, setDateFilters] = useState<string[]>(DEFAULT_RANGE);
  const [appliedRanges, setAppliedRanges] = useState<string[]>(DEFAULT_RANGE);
  const [custom, setCustom] = useState<Custom>(NO_CUSTOM);
  const [showCustomRange, setShowCustomRange] = useState(false);

  const [walletForm, setWalletForm] = useState<{ wallet: Wallet | null } | null>(null);
  const [transactionForm, setTransactionForm] = useState<{ transaction: Transaction | null } | null>(null);
  const [showTransfer, setShowTransfer] = useState(false);
  const [deletingWallet, setDeletingWallet] = useState<Wallet | null>(null);
  const [deletingTransaction, setDeletingTransaction] = useState<Transaction | null>(null);

  const dateRange = useMemo(() => mergeDateRanges(appliedRanges, custom), [appliedRanges, custom]);
  const selectedWallet = selected.length === 1 ? wallets.find((w) => w.id === selected[0]) : undefined;
  const walletIds = selected.length > 0 ? selected : undefined;

  const loadWallets = useCallback(async () => {
    const { data, error: failure } = await api.getWallets();
    setLoading(false);
    setError(failure ? failure.message ?? "Failed to load wallets." : null);
    setWallets(data?.wallets ?? []);
    setTotalBalance(data?.totalBalance ?? 0);
  }, [api]);

  const refresh = () => { loadWallets(); setRefreshKey((k) => k + 1); };

  useEffect(() => { loadWallets(); }, [loadWallets]);
  useEffect(() => { api.getCategories().then(({ data }) => setCategories(data ?? [])); }, [api]);

  useEffect(() => {
    let stale = false;
    setLoadingTransactions(true);
    const hasFilter = typeFilters.length > 0 || categoryIds.length > 0;
    api.getTransactions({ page, limit: PAGE_SIZE, walletIds, type: typeFilters.length === 1 ? typeFilters[0] : undefined, categoryIds: categoryIds.length > 0 ? categoryIds : undefined, dateFrom: dateRange.from ?? undefined, dateTo: dateRange.to ?? undefined, exclude: hasFilter ? "Transfer" : undefined }).then(({ data, error: failure }) => {
      if (stale) return;
      setLoadingTransactions(false);
      if (failure) setError(failure.message ?? "Failed to load transactions.");
      setTransactions(data?.items ?? []);
      setTotal(data?.total ?? 0);
    });
    return () => { stale = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api, selected, typeFilters, categoryIds, dateRange, page, refreshKey]);

  useEffect(() => {
    let stale = false;
    const month = resolveDateRange(DATE_RANGE.thisMonth);
    api.getSummary({ walletIds, dateFrom: month.from ?? undefined, dateTo: month.to ?? undefined }).then(({ data }) => { if (!stale) setSummary({ totalIncome: data?.totalIncome ?? 0, totalExpense: data?.totalExpense ?? 0 }); });
    return () => { stale = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api, selected, refreshKey]);

  const resetFilters = () => {
    setTypeFilters([]);
    setCategoryIds([]);
    setDateFilters(DEFAULT_RANGE);
    setAppliedRanges(DEFAULT_RANGE);
    setCustom(NO_CUSTOM);
    setPage(1);
  };

  const selectAll = () => { setSelected([]); resetFilters(); };
  const toggleWallet = (id: string) => { setSelected((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id])); resetFilters(); };
  const toggleType = (type: TransactionType) => { setTypeFilters((prev) => (prev.includes(type) ? prev.filter((t) => t !== type) : [...prev, type])); setPage(1); };
  const changeCategories = (values: string[]) => { setCategoryIds(values); setPage(1); };

  const changeDateFilters = (values: string[]) => {
    setDateFilters(values);
    if (values.includes(DATE_RANGE.custom) && !appliedRanges.includes(DATE_RANGE.custom)) return setShowCustomRange(true);
    setAppliedRanges(values);
    setPage(1);
  };

  const closeCustomRange = () => { setShowCustomRange(false); setDateFilters(appliedRanges); };
  const applyCustomRange = (value: Custom) => { setCustom(value); setAppliedRanges(dateFilters); setShowCustomRange(false); setPage(1); };

  const typeFilterClass = (active: boolean) => (active ? "btn-primary btn-sm" : "btn-secondary btn-sm");
  const categoryOptions = categories.map((c) => ({ value: c.id, label: c.name, icon: c.icon }));

  const saveWallet = () => { setWalletForm(null); loadWallets(); };
  const saveTransaction = () => { setTransactionForm(null); setPage(1); refresh(); };
  const saveTransfer = () => { setShowTransfer(false); setPage(1); refresh(); };

  const requestDeleteTransaction = (transaction: Transaction) => { setTransactionForm(null); setDeletingTransaction(transaction); };

  const deleteWallet = async () => {
    if (!deletingWallet) return;
    const { id } = deletingWallet;
    setDeletingWallet(null);
    const { error: failure } = await api.deleteWallet(id);
    if (failure) return setError(failure.message ?? "Failed to delete wallet.");
    setSelected((prev) => prev.filter((x) => x !== id));
    refresh();
  };

  const deleteTransaction = async () => {
    if (!deletingTransaction) return;
    const { id } = deletingTransaction;
    setDeletingTransaction(null);
    const { error: failure } = await api.deleteTransaction(id);
    if (failure) return setError(failure.message ?? "Failed to delete transaction.");
    refresh();
  };

  const header = () => {
    if (selected.length === 0) return <><h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">All Transactions</h2><div className="text-sm text-gray-500 dark:text-gray-400">Showing transactions across all wallets</div></>;
    if (selectedWallet) return <><h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{selectedWallet.name}</h2><div className="text-sm text-gray-500 dark:text-gray-400">Balance: <span className="font-semibold text-gray-900 dark:text-gray-100">{formatAmount(selectedWallet.balance)} {selectedWallet.currency}</span></div></>;
    return <><h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Selected Wallets</h2><div className="text-sm text-gray-500 dark:text-gray-400">Showing transactions for {selected.length} wallets</div></>;
  };

  const transactionList = () => {
    if (loadingTransactions && transactions.length === 0) return <Spinner />;
    if (transactions.length === 0) return <div className="text-sm text-gray-400 dark:text-gray-500 py-8 text-center">No transactions found.</div>;
    return (
      <div className="relative">
        {loadingTransactions && <div className="absolute inset-0 z-10 flex items-start justify-center pt-10 bg-white/40 dark:bg-gray-900/40 rounded"><Spinner /></div>}
        <div className={`${loadingTransactions ? "blur-sm pointer-events-none select-none" : ""} flex flex-col divide-y divide-gray-100 dark:divide-gray-700`}>
          {transactions.map((t) => <TransactionRow key={t.id} transaction={t} category={categories.find((c) => c.id === t.categoryId)} onClick={() => setTransactionForm({ transaction: t })} />)}
        </div>
        <Pagination currentPage={page} totalItems={total} pageSize={PAGE_SIZE} onPageChanged={setPage} />
      </div>
    );
  };

  return (
    <>
      {error && <div className={ERROR_BOX}>{error}</div>}
      {loading ? <Spinner /> : (
        <div className="flex flex-col lg:flex-row gap-0 lg:gap-4 mb-6">
          <div className="flex flex-1 flex-col gap-4">
            <div className={`card bg-linear-to-r from-indigo-600 to-indigo-500 text-white cursor-pointer transition-all ${selected.length === 0 ? "ring-4 ring-white/40" : "hover:opacity-90"}`} onClick={selectAll}>
              <div className="text-sm font-medium text-indigo-100 mb-2">Total Balance</div>
              <div className="text-3xl sm:text-4xl font-bold">{formatAmount(totalBalance)}</div>
            </div>

            {wallets.length > 0 && <PieChart wallets={wallets} selectedIds={selected} />}

            <div className="grid grid-cols-2 gap-2">
              <div className="rounded-lg bg-green-50 dark:bg-green-900/30 px-4 py-3">
                <div className="text-xs text-green-600 dark:text-green-400 font-medium mb-0.5">Monthly Income</div>
                <div className="text-xl font-bold text-green-700 dark:text-green-400">{formatAmount(summary.totalIncome)}</div>
              </div>
              <div className="rounded-lg bg-red-50 dark:bg-red-900/30 px-4 py-3">
                <div className="text-xs text-red-600 dark:text-red-400 font-medium mb-0.5">Monthly Expense</div>
                <div className="text-xl font-bold text-red-700 dark:text-red-400">{formatAmount(summary.totalExpense)}</div>
              </div>
            </div>

            <div className="grid grid-cols-3 gap-2 mb-3">
              {wallets.map((w) => <WalletTile key={w.id} wallet={w} selected={selected.includes(w.id)} onSelect={() => toggleWallet(w.id)} onEdit={() => setWalletForm({ wallet: w })} onDelete={() => setDeletingWallet(w)} />)}
              <div className="rounded-lg border-2 border-dashed border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 hover:border-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 cursor-pointer transition-all p-3 flex flex-col items-center justify-center gap-1.5 text-gray-400 dark:text-gray-500 hover:text-indigo-500 dark:hover:text-indigo-400" onClick={() => setWalletForm({ wallet: null })}>
                <SvgIcon className="w-5 h-5" path={ICON_PATHS.plus} />
                <span className="text-xs font-medium">Add Wallet</span>
              </div>
            </div>
          </div>

          <div className="flex-1">
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-4"><div>{header()}</div></div>

            <div className="mb-4 flex flex-col gap-3">
              <div className="flex flex-wrap gap-2">
                <button className={typeFilterClass(typeFilters.length === 0)} onClick={resetFilters}>All</button>
                <button className={typeFilterClass(typeFilters.includes(TRANSACTION_TYPE.income))} onClick={() => toggleType(TRANSACTION_TYPE.income)}>Income</button>
                <button className={typeFilterClass(typeFilters.includes(TRANSACTION_TYPE.expense))} onClick={() => toggleType(TRANSACTION_TYPE.expense)}>Expense</button>
                <MultiSelectInput values={categoryIds} onChange={changeCategories} options={categoryOptions} placeholder="All categories" />
                <MultiSelectInput values={dateFilters} onChange={changeDateFilters} options={DATE_RANGE_OPTIONS} placeholder="All time" />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-2 mb-4">
              <button className="btn-primary btn w-full" onClick={() => setTransactionForm({ transaction: null })}><SvgIcon className="w-3.5 h-3.5 mr-1" path={ICON_PATHS.plus} />Add Transaction</button>
              <button className="btn-secondary btn w-full" onClick={() => setShowTransfer(true)}><SvgIcon className="w-3.5 h-3.5 mr-1" path={ICON_PATHS.transfer} />Transfer</button>
            </div>

            <div className="card px-3 py-1">{transactionList()}</div>
          </div>
        </div>
      )}

      {walletForm && <WalletModal wallet={walletForm.wallet} onClose={() => setWalletForm(null)} onSaved={saveWallet} />}
      {transactionForm && <TransactionModal transaction={transactionForm.transaction} wallets={wallets} categories={categories} defaultWalletId={selectedWallet?.id ?? wallets[0]?.id ?? ""} onClose={() => setTransactionForm(null)} onSaved={saveTransaction} onDelete={requestDeleteTransaction} />}
      {showTransfer && <TransferModal wallets={wallets} defaultFromId={selectedWallet?.id ?? ""} onClose={() => setShowTransfer(false)} onSaved={saveTransfer} />}
      {showCustomRange && <CustomRangeModal initial={{ from: dateRange.from, to: dateRange.to }} onClose={closeCustomRange} onApply={applyCustomRange} />}

      <ConfirmDialog visible={!!deletingWallet} title="Delete wallet" message={`Delete '${deletingWallet?.name}'?`} onConfirm={deleteWallet} onCancel={() => setDeletingWallet(null)} />
      <ConfirmDialog visible={!!deletingTransaction} title="Delete transaction" message={`Delete '${deletingTransaction?.note ?? deletingTransaction?.categoryName ?? "this transaction"}'?`} onConfirm={deleteTransaction} onCancel={() => setDeletingTransaction(null)} />
    </>
  );
};
