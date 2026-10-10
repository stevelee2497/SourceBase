import { useState } from "react";
import { Modal } from "../../components/Modal";
import type { Custom } from "../../lib/dateRanges";

const RANGE_ERROR = "From date must be earlier than or equal to To date.";

type Props = { initial: Custom; onClose: () => void; onApply: (custom: Custom) => void };

export const CustomRangeModal = ({ initial, onClose, onApply }: Props) => {
  const [from, setFrom] = useState(initial.from ?? "");
  const [to, setTo] = useState(initial.to ?? "");
  const [error, setError] = useState<string | null>(null);

  const apply = () => {
    if (from && to && from > to) return setError(RANGE_ERROR);
    onApply({ from: from || null, to: to || null });
  };

  return (
    <Modal title="Custom Date Range" onClose={onClose} error={error} className="max-w-md p-4 md:p-6">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div>
          <label className="form-label">From</label>
          <input type="date" className="form-input" value={from} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div>
          <label className="form-label">To</label>
          <input type="date" className="form-input" value={to} onChange={(e) => setTo(e.target.value)} />
        </div>
      </div>
      <div className="mt-5 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
        <button className="btn-secondary w-full sm:w-auto" onClick={onClose}>Cancel</button>
        <button className="btn-primary w-full sm:w-auto" onClick={apply}>Apply</button>
      </div>
    </Modal>
  );
};
