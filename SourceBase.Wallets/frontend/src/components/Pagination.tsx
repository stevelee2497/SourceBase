type Props = { currentPage: number; totalItems: number; pageSize: number; onPageChanged: (page: number) => void };

export const Pagination = ({ currentPage, totalItems, pageSize, onPageChanged }: Props) => {
  const totalPages = pageSize > 0 ? Math.ceil(totalItems / pageSize) : 1;
  if (totalPages <= 1) return null;
  const start = Math.max(1, currentPage - 2);
  const end = Math.min(totalPages, currentPage + 2);
  const pages = Array.from({ length: end - start + 1 }, (_, i) => start + i);

  return (
    <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between px-2 py-3 sm:px-0 mt-4 gap-2">
      <div className="text-sm text-gray-500 dark:text-gray-400 text-center sm:text-left">Page {currentPage} of {totalPages} ({totalItems} items)</div>
      <div className="flex items-center justify-center gap-1 flex-wrap">
        <button className="btn-secondary btn-sm" disabled={currentPage <= 1} onClick={() => onPageChanged(currentPage - 1)}>← Prev</button>
        {pages.map((p) => <button key={p} className={p === currentPage ? "btn-primary btn-sm" : "btn-secondary btn-sm"} onClick={() => onPageChanged(p)}>{p}</button>)}
        <button className="btn-secondary btn-sm" disabled={currentPage >= totalPages} onClick={() => onPageChanged(currentPage + 1)}>Next →</button>
      </div>
    </div>
  );
};
