import { useEffect, useState } from "react";
import { useApi } from "../lib/api";
import type { IconItem } from "../lib/types";
import { Icon } from "./Icon";

type Props = { group: "Wallet" | "Category"; value: string; onChange: (value: string) => void; className?: string };

export const IconPicker = ({ group, value, onChange, className = "" }: Props) => {
  const api = useApi();
  const [icons, setIcons] = useState<IconItem[] | null>(null);

  useEffect(() => {
    api.getIcons(group).then(({ data }) => {
      setIcons(data ?? []);
      if (!value && data?.length) onChange(data[0].value);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api, group]);

  if (!icons) return <div className={className}><div className="py-4 text-center text-sm text-gray-400 dark:text-gray-500">Loading icons…</div></div>;
  return (
    <div className={className}>
      <div className="rounded-md border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-2">
        <div className="grid grid-cols-6 gap-1">
          {icons.map((icon) => (
            <button key={icon.id} type="button" title={icon.name} onClick={() => onChange(icon.value)} className={`flex items-center justify-center p-1.5 rounded transition-colors ${icon.value === value ? "bg-indigo-50 dark:bg-indigo-900/30 ring-1 ring-indigo-400" : "hover:bg-gray-100 dark:hover:bg-gray-700"}`}>
              <Icon value={icon.value} emojiClass="text-xl leading-none" />
            </button>
          ))}
        </div>
      </div>
    </div>
  );
};
