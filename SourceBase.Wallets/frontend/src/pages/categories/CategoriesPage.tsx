import { useCallback, useEffect, useState } from "react";
import { useApi } from "../../lib/api";
import type { Category, TransactionType } from "../../lib/types";
import { ConfirmDialog } from "../../components/ConfirmDialog";
import { Icon } from "../../components/Icon";
import { Spinner } from "../../components/Spinner";
import { CategoryModal } from "./CategoryModal";
import { DELETE_PATH, EDIT_PATH, Glyph, IconLibrary, LOCK_PATH } from "./IconLibrary";

const DEFAULT_ICON = "🏷️";
const ACTION = "text-gray-400 dark:text-gray-500 transition-colors";

const STYLES: Record<TransactionType, { title: string; empty: string; badge: string; circle: string }> = {
  Income: { title: "Income Categories", empty: "No income categories yet.", badge: "badge-green", circle: "bg-green-50 dark:bg-green-900/30 text-green-600 dark:text-green-400" },
  Expense: { title: "Expense Categories", empty: "No expense categories yet.", badge: "badge-red", circle: "bg-red-50 dark:bg-red-900/30 text-red-600 dark:text-red-400" },
};

type RowProps = { category: Category; onEdit: (category: Category) => void; onDelete: (category: Category) => void };

const CategoryRow = ({ category, onEdit, onDelete }: RowProps) => {
  const style = STYLES[category.type];
  return (
    <div className="flex items-center justify-between gap-3 rounded-lg border border-gray-100 dark:border-gray-700 px-4 py-3">
      <div className="flex items-center gap-3 min-w-0">
        <div className={`w-10 h-10 rounded-full ${style.circle} flex items-center justify-center text-lg shrink-0`}>
          <Icon value={category.icon} fallback={DEFAULT_ICON} emojiClass="text-lg leading-none" />
        </div>
        <div className="min-w-0">
          <div className="font-medium text-gray-900 dark:text-gray-100 truncate">{category.name}</div>
          <div className="flex flex-wrap items-center gap-2 mt-1">
            <span className={style.badge}>{category.type}</span>
            {category.isSystem && <span className="badge-blue">System</span>}
          </div>
        </div>
      </div>
      <div className="flex items-center gap-2 shrink-0">
        {category.isSystem ? (
          <span className="text-gray-400 dark:text-gray-500" title="System category"><Glyph d={LOCK_PATH} size="w-4 h-4" /></span>
        ) : (
          <>
            <button className={`${ACTION} hover:text-indigo-600 dark:hover:text-indigo-400`} onClick={() => onEdit(category)} title="Edit category"><Glyph d={EDIT_PATH} size="w-4 h-4" /></button>
            <button className={`${ACTION} hover:text-red-600 dark:hover:text-red-400`} onClick={() => onDelete(category)} title="Delete category"><Glyph d={DELETE_PATH} size="w-4 h-4" /></button>
          </>
        )}
      </div>
    </div>
  );
};

export const CategoriesPage = () => {
  const api = useApi();
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState<{ category: Category | null } | null>(null);
  const [deleting, setDeleting] = useState<Category | null>(null);

  const load = useCallback(async () => {
    const { data } = await api.getCategories();
    setCategories(data ?? []);
    setLoading(false);
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const saved = () => { setForm(null); load(); };

  const confirmDelete = async () => {
    if (!deleting) return;
    const { error } = await api.deleteCategory(deleting.id);
    setDeleting(null);
    if (!error) load();
  };

  return (
    <>
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Categories</h1>
          <div className="text-sm text-gray-500 dark:text-gray-400 mt-1">Manage your custom income and expense categories.</div>
        </div>
        <button className="btn-primary self-start sm:self-auto" onClick={() => setForm({ category: null })}>
          <Glyph d="M12 4v16m8-8H4" size="w-4 h-4 mr-1.5" />
          Add Category
        </button>
      </div>

      {loading ? <Spinner /> : (
        <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
          {(Object.keys(STYLES) as TransactionType[]).map((type) => {
            const style = STYLES[type];
            const items = categories.filter((c) => c.type === type);
            return (
              <section key={type} className="card">
                <div className="flex items-center justify-between gap-3 mb-4">
                  <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{style.title}</h2>
                  <span className={style.badge}>{items.length}</span>
                </div>
                {items.length === 0 ? <div className="text-sm text-gray-400 dark:text-gray-500 py-6 text-center">{style.empty}</div> : (
                  <div className="flex flex-col gap-3">
                    {items.map((c) => <CategoryRow key={c.id} category={c} onEdit={(category) => setForm({ category })} onDelete={setDeleting} />)}
                  </div>
                )}
              </section>
            );
          })}
        </div>
      )}

      <IconLibrary />

      {form && <CategoryModal category={form.category} onClose={() => setForm(null)} onSaved={saved} />}
      <ConfirmDialog visible={!!deleting} title="Delete category" message={`Delete '${deleting?.name}'?`} onConfirm={confirmDelete} onCancel={() => setDeleting(null)} />
    </>
  );
};
