import { format, parseISO } from "date-fns";

/** DateOnly -> "yyyy-MM-dd" */
export const toApiDate = (d: Date) => format(d, "yyyy-MM-dd");

/** DateTime -> ISO 8601 */
export const toApiDateTime = (d: Date) => d.toISOString();

export const fromApiDate = (s: string) => parseISO(s);

export const labelDate = (s: string) => format(parseISO(s), "EEE, MMM d");

export const dayKey = (iso: string) => format(parseISO(iso), "yyyy-MM-dd");

export const dayHeading = (iso: string) => format(parseISO(iso), "EEEE, MMM d");

export const timeLabel = (iso: string) => format(parseISO(iso), "HH:mm");
