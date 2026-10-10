import { useState } from "react";
import { useApi } from "../../lib/api";
import type { IconGroup, IconItem } from "../../lib/types";
import { Icon } from "../../components/Icon";
import { Modal, ModalActions } from "../../components/Modal";
import { SelectInput } from "../../components/SelectInput";

export type IconMode = "emoji" | "svg" | "image";

const MODES: { mode: IconMode; label: string }[] = [{ mode: "emoji", label: "Emoji" }, { mode: "svg", label: "SVG Path" }, { mode: "image", label: "Image Upload" }];
const GROUP_OPTIONS = (["Wallet", "Category", "General"] as IconGroup[]).map((g) => ({ value: g, label: g }));
const DEFAULT_GROUP: IconGroup = "General";
const ACCEPT = "image/jpeg,image/png,image/gif,image/webp,image/svg+xml";
const MAX_UPLOAD_BYTES = 5 * 1024 * 1024;
const MODE_ACTIVE = "border-indigo-500 bg-indigo-50 dark:bg-indigo-900/40 text-indigo-700 dark:text-indigo-400";
const MODE_IDLE = "border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:border-gray-400 dark:hover:border-gray-500";
const FILE_INPUT = "block w-full text-sm text-gray-500 dark:text-gray-400 file:mr-3 file:py-1.5 file:px-3 file:rounded file:border file:border-gray-300 dark:file:border-gray-600 file:text-sm file:bg-white dark:file:bg-gray-800 file:text-gray-700 dark:file:text-gray-200 hover:file:border-gray-400 dark:hover:file:border-gray-500";
const SVG_PLACEHOLDER = '<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M..." />';

export const detectMode = (value: string): IconMode => value.trimStart().startsWith("<") ? "svg" : value.startsWith("http") ? "image" : "emoji";

type Props = { icon: IconItem | null; onClose: () => void; onSaved: () => void };

export const IconModal = ({ icon, onClose, onSaved }: Props) => {
  const api = useApi();
  const [name, setName] = useState(icon?.name ?? "");
  const [group, setGroup] = useState<IconGroup>(icon?.group ?? DEFAULT_GROUP);
  const [sortOrder, setSortOrder] = useState(icon?.sortOrder ?? 0);
  const [value, setValue] = useState(icon?.value ?? "");
  const [mode, setMode] = useState<IconMode>(icon ? detectMode(icon.value) : "emoji");
  const [uploading, setUploading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const switchMode = (next: IconMode) => { setMode(next); setValue(""); };

  const upload = async (file?: File) => {
    if (!file) return;
    if (file.size > MAX_UPLOAD_BYTES) return setError("Image must be 5 MB or smaller.");
    setError(null);
    setUploading(true);
    const { data } = await api.uploadIcon(file);
    setUploading(false);
    if (data) setValue(data.iconUrl);
  };

  const save = async () => {
    if (!name.trim()) return setError("Name is required.");
    if (!value.trim()) return setError("Icon value is required.");
    setError(null);
    setSaving(true);
    const body = { value: value.trim(), name: name.trim(), group, sortOrder };
    const { error: failed } = icon ? await api.updateIcon(icon.id, body) : await api.createIcon(body);
    setSaving(false);
    if (!failed) onSaved();
  };

  return (
    <Modal title={icon ? "Edit Icon" : "Add Icon"} onClose={onClose} error={error} className="max-w-lg p-6" footer={<ModalActions onCancel={onClose} onSave={save} saving={saving} />}>
      <div>
        <label className="form-label">Name *</label>
        <input value={name} onChange={(e) => setName(e.target.value)} className="form-input" placeholder="Icon name" />
      </div>
      <div>
        <label className="form-label">Group</label>
        <SelectInput value={group} onChange={(v) => setGroup(v as IconGroup)} options={GROUP_OPTIONS} />
      </div>
      <div>
        <label className="form-label">Sort Order</label>
        <input value={sortOrder} onChange={(e) => setSortOrder(Number(e.target.value) || 0)} className="form-input" type="number" min="0" />
      </div>
      <div>
        <label className="form-label">Icon Value</label>
        <div className="flex gap-2 mb-2">
          {MODES.map((m) => <button key={m.mode} type="button" className={`text-xs px-2 py-1 rounded border ${mode === m.mode ? MODE_ACTIVE : MODE_IDLE}`} onClick={() => switchMode(m.mode)}>{m.label}</button>)}
        </div>
        {mode === "emoji" && <input value={value} onChange={(e) => setValue(e.target.value)} className="form-input" placeholder="e.g. 💳" />}
        {mode === "svg" && (
          <>
            <textarea value={value} onChange={(e) => setValue(e.target.value)} className="form-input font-mono text-xs" rows={4} placeholder={SVG_PLACEHOLDER}></textarea>
            <div className="text-xs text-gray-400 dark:text-gray-500 mt-1">Paste the <code>&lt;path&gt;</code> content from a 24×24 SVG (e.g. Heroicons).</div>
          </>
        )}
        {mode === "image" && (
          <>
            {uploading ? <div className="text-sm text-gray-500 dark:text-gray-400 py-2">Uploading…</div> : <input type="file" accept={ACCEPT} onChange={(e) => upload(e.target.files?.[0])} className={FILE_INPUT} />}
            {value.startsWith("http") && <div className="mt-2 text-xs text-gray-500 dark:text-gray-400 truncate">{value}</div>}
          </>
        )}
        {value && (
          <div className="mt-3 flex items-center gap-3">
            <span className="text-xs text-gray-500 dark:text-gray-400">Preview:</span>
            <div className="w-10 h-10 flex items-center justify-center rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 text-gray-700 dark:text-gray-200">
              <Icon value={value} size="w-6 h-6" emojiClass="text-2xl leading-none" />
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
};
