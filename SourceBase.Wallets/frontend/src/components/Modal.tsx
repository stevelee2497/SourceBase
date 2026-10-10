import type { ReactNode } from "react";

type Props = { title: string; onClose: () => void; children: ReactNode; error?: string | null; footer?: ReactNode; className?: string };

export const Modal = ({ title, onClose, children, error, footer, className = "max-w-md p-6" }: Props) => (
  <div className="fixed inset-0 z-40 flex items-center justify-center">
    <div className="absolute inset-0 bg-black/40" onClick={onClose}></div>
    <div className={`relative bg-white dark:bg-gray-800 rounded-lg shadow-xl w-full mx-4 ${className}`} style={{ maxHeight: "90vh", overflowY: "auto" }}>
      <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">{title}</h2>
      {error && <div className="mb-3 rounded-md bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 px-3 py-2 text-sm text-red-700 dark:text-red-400">{error}</div>}
      <div className="space-y-4">{children}</div>
      {footer && <div className="flex justify-end gap-3 mt-5">{footer}</div>}
    </div>
  </div>
);

type ActionsProps = { onCancel: () => void; onSave: () => void; saving?: boolean; saveDisabled?: boolean; saveLabel?: string; left?: ReactNode };

/** Standard Cancel / Save footer. `left` is for an extra button (e.g. Delete) pushed to the left edge. */
export const ModalActions = ({ onCancel, onSave, saving, saveDisabled, saveLabel = "Save", left }: ActionsProps) => (
  <>
    {left && <div className="mr-auto">{left}</div>}
    <button className="btn-secondary" onClick={onCancel}>Cancel</button>
    <button className="btn-primary" onClick={onSave} disabled={saving || saveDisabled}>{saving ? "Saving…" : saveLabel}</button>
  </>
);
