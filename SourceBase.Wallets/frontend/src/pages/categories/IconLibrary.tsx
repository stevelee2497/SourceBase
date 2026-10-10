import { useCallback, useEffect, useState } from "react";
import { useApi } from "../../lib/api";
import type { IconGroup, IconItem } from "../../lib/types";
import { ConfirmDialog } from "../../components/ConfirmDialog";
import { Icon } from "../../components/Icon";
import { Spinner } from "../../components/Spinner";
import { IconModal } from "./IconModal";

export const LOCK_PATH = "M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 10-8 0v4h8z";
export const EDIT_PATH = "M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z";
export const DELETE_PATH = "M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16";
const ADD_PATH = "M12 4v16m8-8H4";

export const Glyph = ({ d, size }: { d: string; size: string }) => (
  <svg className={size} fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d={d} /></svg>
);

type Section = { title: string; empty: string; badge: string; groups: IconGroup[] };
const SECTIONS: Section[] = [
  { title: "Wallet Icons", empty: "No wallet icons yet.", badge: "badge-blue", groups: ["Wallet", "General"] },
  { title: "Category Icons", empty: "No category icons yet.", badge: "badge-green", groups: ["Category", "General"] },
];
const ACTION = "text-gray-400 dark:text-gray-500 transition-colors";

type TileProps = { icon: IconItem; onEdit: (icon: IconItem) => void; onDelete: (icon: IconItem) => void };

const IconTile = ({ icon, onEdit, onDelete }: TileProps) => (
  <div className="relative group flex flex-col items-center gap-1 rounded-lg border border-gray-100 dark:border-gray-700 p-2 hover:border-gray-300 dark:hover:border-gray-600 transition-colors">
    <div className="w-10 h-10 flex items-center justify-center text-gray-700 dark:text-gray-200">
      <Icon value={icon.value} size="w-6 h-6" emojiClass="text-2xl leading-none" />
    </div>
    <span className="text-xs text-gray-500 dark:text-gray-400 text-center leading-tight truncate w-full">{icon.name}</span>
    {icon.isSystem ? (
      <span className="absolute top-1 right-1 text-gray-300 dark:text-gray-500" title="System icon"><Glyph d={LOCK_PATH} size="w-3 h-3" /></span>
    ) : (
      <div className="absolute top-1 right-1 hidden group-hover:flex gap-0.5">
        <button className={`${ACTION} hover:text-indigo-600 dark:hover:text-indigo-400`} onClick={() => onEdit(icon)} title="Edit"><Glyph d={EDIT_PATH} size="w-3 h-3" /></button>
        <button className={`${ACTION} hover:text-red-600 dark:hover:text-red-400`} onClick={() => onDelete(icon)} title="Delete"><Glyph d={DELETE_PATH} size="w-3 h-3" /></button>
      </div>
    )}
  </div>
);

export const IconLibrary = () => {
  const api = useApi();
  const [icons, setIcons] = useState<IconItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState<{ icon: IconItem | null } | null>(null);
  const [deleting, setDeleting] = useState<IconItem | null>(null);

  const load = useCallback(async () => {
    const { data } = await api.getIcons();
    setIcons(data ?? []);
    setLoading(false);
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const saved = () => { setForm(null); load(); };

  const confirmDelete = async () => {
    if (!deleting) return;
    const { error } = await api.deleteIcon(deleting.id);
    setDeleting(null);
    if (!error) load();
  };

  return (
    <section className="card mt-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-4">
        <div>
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Icon Library</h2>
          <div className="text-sm text-gray-500 dark:text-gray-400 mt-1">Browse and manage icons for wallets and categories.</div>
        </div>
        <button className="btn-primary self-start sm:self-auto" onClick={() => setForm({ icon: null })}>
          <Glyph d={ADD_PATH} size="w-4 h-4 mr-1.5" />
          Add Icon
        </button>
      </div>
      {loading ? <Spinner /> : (
        <div className="space-y-6">
          {SECTIONS.map((s) => {
            const items = icons.filter((i) => s.groups.includes(i.group));
            return (
              <div key={s.title}>
                <div className="flex items-center justify-between gap-3 mb-4">
                  <h3 className="text-base font-semibold text-gray-900 dark:text-gray-100">{s.title}</h3>
                  <span className={s.badge}>{items.length}</span>
                </div>
                {items.length === 0 ? <div className="text-sm text-gray-400 dark:text-gray-500 py-6 text-center">{s.empty}</div> : (
                  <div className="grid grid-cols-4 sm:grid-cols-6 gap-2">
                    {items.map((icon) => <IconTile key={icon.id} icon={icon} onEdit={(i) => setForm({ icon: i })} onDelete={setDeleting} />)}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
      {form && <IconModal icon={form.icon} onClose={() => setForm(null)} onSaved={saved} />}
      <ConfirmDialog visible={!!deleting} title="Delete icon" message={`Delete '${deleting?.name}'?`} onConfirm={confirmDelete} onCancel={() => setDeleting(null)} />
    </section>
  );
};
