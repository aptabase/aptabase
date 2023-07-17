import { formatDuration, intervalToDuration } from "date-fns";

const formatDistanceLocale: Record<string, string> = {
  xSeconds: "{{count}}s",
  xMinutes: "{{count}}m",
  xHours: "{{count}}h",
};

const durationFormat = {
  format: ["hours", "minutes", "seconds"],
  locale: {
    formatDistance: (token, count) =>
      formatDistanceLocale[token].replace("{{count}}", count),
  } as Locale,
};

function formatLargeNumber(num: number): string {
  if (num >= 1e6) {
    return (num / 1e6).toFixed(2) + "M";
  } else if (num >= 1e3) {
    return (num / 1e3).toFixed(1) + "k";
  }
  return num.toString();
}

export function formatNumber(
  value: number | undefined,
  format?: "number" | "duration"
): string {
  if (format === "duration") {
    if (value === 0) return "0s";

    const interval = intervalToDuration({
      start: 0,
      end: (value ?? 0) * 1000,
    });

    return formatDuration(interval, durationFormat);
  }

  return formatLargeNumber(value ?? 0);
}
