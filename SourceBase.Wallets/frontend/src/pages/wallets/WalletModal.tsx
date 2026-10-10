import { useState } from "react";
import { IconPicker } from "../../components/IconPicker";
import { Modal, ModalActions } from "../../components/Modal";
import { useApi } from "../../lib/api";
import type { Wallet } from "../../lib/types";
import { DEFAULT_CURRENCY, optionalText } from "./constants";

const HINT = "text-xs text-gray-400 dark:text-gray-500 mt-1";

type Props = { wallet: Wallet | null; onClose: () => void; onSaved: () => void };

export const WalletModal = ({ wallet, onClose, onSaved }: Props) => {
  const api = useApi();
  const [name, setName] = useState(wallet?.name ?? "");
  const [initialBalance, setInitialBalance] = useState(String(wallet?.initialBalance ?? 0));
  const [currency, setCurrency] = useState(wallet?.currency ?? DEFAULT_CURRENCY);
  const [icon, setIcon] = useState(wallet?.icon ?? "");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const send = async () => {
    const body = { name: name.trim(), icon: optionalText(icon) };
    if (!wallet) return api.createWallet({ ...body, initialBalance: Number(initialBalance) || 0, currency: currency.trim() });
    const updated = await api.updateWallet(wallet.id, body);
    if (updated.error || currency.trim() === wallet.currency) return updated;
    return api.configureWallet(wallet.id, currency.trim());
  };

  const save = async () => {
    if (!name.trim()) return setError("Name is required.");
    if (!currency.trim()) return setError("Currency is required.");
    setError(null);
    setSaving(true);
    const { error: failure } = await send();
    setSaving(false);
    if (failure) return setError(failure.message ?? "Failed to save wallet.");
    onSaved();
  };

  return (
    <Modal title={wallet ? "Edit Wallet" : "Add Wallet"} onClose={onClose} error={error} footer={<ModalActions onCancel={onClose} onSave={save} saving={saving} />}>
      <div>
        <label className="form-label">Name *</label>
        <input className="form-input" value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g. Main Account" />
      </div>
      <div>
        <label className="form-label">Opening Balance</label>
        <input className="form-input" type="number" step="0.01" value={initialBalance} onChange={(e) => setInitialBalance(e.target.value)} disabled={!!wallet} />
        {wallet && <div className={HINT}>Opening balance can only be set when the wallet is created.</div>}
      </div>
      <div>
        <label className="form-label">Currency *</label>
        <input className="form-input" value={currency} onChange={(e) => setCurrency(e.target.value)} placeholder="USD" />
      </div>
      <div>
        <label className="form-label">Icon</label>
        <IconPicker group="Wallet" value={icon} onChange={setIcon} />
      </div>
    </Modal>
  );
};
