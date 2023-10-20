import { hourCycle } from "@features/env";
import { format, parseJSON } from "date-fns";
import { enUS } from "date-fns/locale";

export function formatDate(value: Date | string | undefined): string {
  if (!value) return "";

  return format(new Date(value), "PP", { locale: enUS });
}

export function formatTime(value: Date | string | undefined): string {
  if (!value) return "";

  return format(new Date(value), "pp", { locale: enUS });
}

const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

export function formatPeriod(granularity: "hour" | "day" | "month", period: string) {
  try {
    if (granularity === "hour") {
      return format(parseJSON(period), hourCycle === "h12" ? "haaaaa'm'" : "HH:mm");
    }

    const [year, month, day] = period.substring(0, 10).split("-");
    const monthName = months[parseInt(month, 10) - 1];

    switch (granularity) {
      case "day":
        return `${monthName} ${day}`;
      case "month":
        return `${monthName} ${year}`;
    }
  } catch (e) {
    return period;
  }
}
