import { useState } from "react";
import { CurrencyInput } from "../../components/CurrencyInput";
import { Modal, ModalActions } from "../../components/Modal";
import { SelectInput, type SelectOption } from "../../components/SelectInput";
import { useApi } from "../../lib/api";
import { todayIso } from "../../lib/format";
import type { Category, Transaction, TransactionType, Wallet } from "../../lib/types";
import { TRANSACTION_TYPE, optionalText, walletOption } from "./constants";

const TYPE_OPTIONS: SelectOption[] = [{ value: TRANSACTION_TYPE.income, label: "🪴 Income" }, { value: TRANSACTION_TYPE.expense, label: "💸 Expense" }];
const NO_CATEGORY: SelectOption = { value: "", label: "Select category" };

type Props = { transaction: Transaction | null; wallets: Wallet[]; categories: Category[]; defaultWalletId: string; onClose: () => void; onSaved: () => void; onDelete: (transaction: Transaction) => void };

export const TransactionModal = ({ transaction, wallets, categories, defaultWalletId, onClose, onSaved, onDelete }: Props) => {
  const api = useApi();
  const [amount, setAmount] = useState(transaction?.amount ?? 0);
  const [type, setType] = useState<TransactionType>(transaction?.type ?? TRANSACTION_TYPE.expense);
  const [date, setDate] = useState(transaction?.date ?? todayIso());
  const [note, setNote] = useState(transaction?.note ?? "");
  const [categoryId, setCategoryId] = useState(transaction?.categoryId ?? "");
  const [walletId, setWalletId] = useState(transaction?.walletId ?? defaultWalletId);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const typeCategories = categories.filter((c) => c.type.toLowerCase() === type.toLowerCase());
  const categoryOptions = [NO_CATEGORY, ...typeCategories.map((c) => ({ value: c.id, label: c.name, icon: c.icon }))];

  const changeType = (value: string) => {
    const next = value as TransactionType;
    setType(next);
    if (!categories.some((c) => c.id === categoryId && c.type.toLowerCase() === next.toLowerCase())) setCategoryId("");
  };

  const validate = () => {
    if (amount <= 0) return "Amount must be greater than zero.";
    if (!date) return "Date is required.";
    if (!categoryId) return "Category is required.";
    if (!walletId) return "Wallet is required.";
    return null;
  };

  const save = async () => {
    const invalid = validate();
    setError(invalid);
    if (invalid) return;
    setSaving(true);
    const body = { walletId, amount, type, date, note: optionalText(note), categoryId };
    const { error: failure } = transaction ? await api.updateTransaction(transaction.id, body) : await api.createTransaction(body);
    setSaving(false);
    if (failure) return setError(failure.message ?? "Failed to save transaction.");
    onSaved();
  };

  const left = transaction && <button className="btn-danger" onClick={() => onDelete(transaction)}>Delete</button>;

  return (
    <Modal title={transaction ? "Edit Transaction" : "Add Transaction"} onClose={onClose} error={error} className="max-w-md p-4 md:p-6" footer={<ModalActions onCancel={onClose} onSave={save} saving={saving} left={left} />}>
      <div className="grid grid-cols-2 gap-2">
        <div>
          <label className="form-label">Amount *</label>
          <CurrencyInput value={amount} onChange={setAmount} className="form-input" />
        </div>
        <div>
          <label className="form-label">Date *</label>
          <input className="form-input" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        </div>
      </div>
      <div className="grid grid-cols-2 gap-2">
        <div>
          <label className="form-label">Type</label>
          <SelectInput value={type} onChange={changeType} options={TYPE_OPTIONS} />
        </div>
        <div>
          <label className="form-label">Category *</label>
          <SelectInput value={categoryId} onChange={setCategoryId} options={categoryOptions} />
        </div>
      </div>
      <div>
        <label className="form-label">Note</label>
        <textarea className="form-input" rows={2} placeholder="Optional description" value={note} onChange={(e) => setNote(e.target.value)} />
      </div>
      <div>
        <label className="form-label">Wallet</label>
        <SelectInput value={walletId} onChange={setWalletId} options={wallets.map(walletOption)} />
      </div>
    </Modal>
  );
};
