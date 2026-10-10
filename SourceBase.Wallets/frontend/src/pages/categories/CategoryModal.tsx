import { useState } from "react";
import { useApi } from "../../lib/api";
import type { Category, TransactionType } from "../../lib/types";
import { IconPicker } from "../../components/IconPicker";
import { Modal, ModalActions } from "../../components/Modal";
import { SelectInput } from "../../components/SelectInput";

const TYPE_OPTIONS = [{ value: "Income", label: "Income" }, { value: "Expense", label: "Expense" }];
const DEFAULT_TYPE: TransactionType = "Expense";

type Props = { category: Category | null; onClose: () => void; onSaved: () => void };

export const CategoryModal = ({ category, onClose, onSaved }: Props) => {
  const api = useApi();
  const [name, setName] = useState(category?.name ?? "");
  const [type, setType] = useState<TransactionType>(category?.type ?? DEFAULT_TYPE);
  const [icon, setIcon] = useState(category?.icon ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const save = async () => {
    if (!name.trim()) return setError("Name is required.");
    setError(null);
    setSaving(true);
    const iconValue = icon.trim() || undefined;
    const { error: failed } = category ? await api.updateCategory(category.id, { name: name.trim(), icon: iconValue }) : await api.createCategory({ name: name.trim(), type, icon: iconValue });
    setSaving(false);
    if (!failed) onSaved();
  };

  return (
    <Modal title={category ? "Edit Category" : "Add Category"} onClose={onClose} error={error} footer={<ModalActions onCancel={onClose} onSave={save} saving={saving} />}>
      <div>
        <label className="form-label">Name *</label>
        <input value={name} onChange={(e) => setName(e.target.value)} className="form-input" placeholder="Category name" />
      </div>
      <div>
        <label className="form-label">Type</label>
        <SelectInput value={type} onChange={(v) => setType(v as TransactionType)} options={TYPE_OPTIONS} disabled={!!category} />
        {category && <div className="text-xs text-gray-400 dark:text-gray-500 mt-1">Category type can only be chosen when the category is created.</div>}
      </div>
      <div>
        <label className="form-label">Icon</label>
        <IconPicker group="Category" value={icon} onChange={setIcon} />
      </div>
    </Modal>
  );
};
