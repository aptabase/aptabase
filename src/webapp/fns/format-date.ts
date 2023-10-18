import { format } from "date-fns";
import { enUS } from "date-fns/locale";

export function formatDate(value: Date | string | undefined): string {
  if (!value) return "";

  return format(new Date(value), "PP", { locale: enUS });
}

export function formatTime(value: Date | string | undefined): string {
  if (!value) return "";

  return format(new Date(value), "pp", { locale: enUS });
}
