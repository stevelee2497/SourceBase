import { useState } from "react";
import { CurrencyInput } from "../../components/CurrencyInput";
import { Modal, ModalActions } from "../../components/Modal";
import { SelectInput } from "../../components/SelectInput";
import { useApi } from "../../lib/api";
import { todayIso } from "../../lib/format";
import type { Wallet } from "../../lib/types";
import { SELECT_WALLET_OPTION, optionalText, walletOption } from "./constants";

type Props = { wallets: Wallet[]; defaultFromId: string; onClose: () => void; onSaved: () => void };

export const TransferModal = ({ wallets, defaultFromId, onClose, onSaved }: Props) => {
  const api = useApi();
  const [fromId, setFromId] = useState(defaultFromId);
  const [toId, setToId] = useState("");
  const [amount, setAmount] = useState(0);
  const [date, setDate] = useState(todayIso());
  const [note, setNote] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const optionsExcluding = (id: string) => [SELECT_WALLET_OPTION, ...wallets.filter((w) => w.id !== id).map(walletOption)];
  const targets = wallets.filter((w) => w.id !== fromId);

  const changeFrom = (id: string) => {
    setFromId(id);
    if (id === toId) setToId("");
  };

  const validate = () => {
    if (!fromId) return "Select a source wallet.";
    if (!toId) return "Select a destination wallet.";
    if (amount <= 0) return "Amount must be greater than zero.";
    if (!date) return "Date is required.";
    return null;
  };

  const save = async () => {
    const invalid = validate();
    setError(invalid);
    if (invalid) return;
    setSaving(true);
    const { error: failure } = await api.createTransfer({ fromWalletId: fromId, toWalletId: toId, amount, date, note: optionalText(note) });
    setSaving(false);
    if (failure) return setError(failure.message ?? "Failed to create transfer.");
    onSaved();
  };

  return (
    <Modal title="Transfer Funds" onClose={onClose} error={error} footer={<ModalActions onCancel={onClose} onSave={save} saving={saving} saveDisabled={targets.length === 0} />}>
      <div>
        <label className="form-label">From Wallet *</label>
        <SelectInput value={fromId} onChange={changeFrom} options={optionsExcluding(toId)} />
      </div>
      <div>
        <label className="form-label">To Wallet *</label>
        <SelectInput value={toId} onChange={setToId} options={optionsExcluding(fromId)} />
      </div>
      <div>
        <label className="form-label">Amount *</label>
        <CurrencyInput value={amount} onChange={setAmount} className="form-input" />
      </div>
      <div>
        <label className="form-label">Date *</label>
        <input className="form-input" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
      </div>
      <div>
        <label className="form-label">Note</label>
        <input className="form-input" placeholder="Optional description" value={note} onChange={(e) => setNote(e.target.value)} />
      </div>
    </Modal>
  );
};
