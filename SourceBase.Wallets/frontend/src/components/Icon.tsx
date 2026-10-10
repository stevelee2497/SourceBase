type Props = { value?: string | null; size?: string; fallback?: string; emojiClass?: string };

/** Renders an icon value: "<path…/>" is an SVG fragment, "http…" an image URL, anything else an emoji. */
export const Icon = ({ value, size = "w-5 h-5", fallback, emojiClass = "text-base leading-none shrink-0" }: Props) => {
  const icon = value || fallback;
  if (!icon) return null;
  if (icon.trimStart().startsWith("<")) return <svg className={`${size} shrink-0`} viewBox="0 0 24 24" fill="none" stroke="currentColor" dangerouslySetInnerHTML={{ __html: icon }} />;
  if (icon.startsWith("http")) return <img src={icon} alt="" className={`${size} shrink-0 object-contain rounded`} />;
  return <span className={emojiClass}>{icon}</span>;
};
