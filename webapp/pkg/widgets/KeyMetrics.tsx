import { Metric } from "./charts";
import { useQuery } from "@tanstack/react-query";
import { formatDuration, intervalToDuration } from "date-fns";
import { useSearchParams } from "react-router-dom";
import { keyMetrics } from "./query";
import { useApps } from "@app/apps";

type Props = {
  appId: string;
  activeMetrics: string[];
  onChangeMetric: (metric: "sessions" | "events") => void;
};

export function KeyMetrics(props: Props) {
  const { buildMode } = useApps();
  const [searchParams] = useSearchParams();
  const period = searchParams.get("period") || "";
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const {
    isLoading,
    isError,
    data: metrics,
  } = useQuery(
    [
      "key-metrics",
      buildMode,
      props.appId,
      period,
      countryCode,
      appVersion,
      eventName,
      osName,
    ],
    () =>
      keyMetrics({
        buildMode,
        appId: props.appId,
        period,
        countryCode,
        appVersion,
        eventName,
        osName,
      })
  );

  const formatDistanceLocale: Record<string, string> = {
    xSeconds: "{{count}}s",
    xMinutes: "{{count}}m",
    xHours: "{{count}}h",
  };

  const shortEnLocale: Locale = {
    formatDistance: (token, count) =>
      formatDistanceLocale[token].replace("{{count}}", count),
  };

  const duration = formatDuration(
    intervalToDuration({
      start: 0,
      end: (metrics?.durationSeconds || 0) * 1000,
    }),
    {
      format: ["hours", "minutes", "seconds"],
      locale: shortEnLocale,
    }
  );

  return (
    <div className="flex justify-between sm:justify-start sm:space-x-4 mb-8">
      {!isLoading && !isError && (
        <>
          <Metric
            label="Sessions"
            value={metrics?.sessions.toString() || "0"}
            activeClassName="bg-primary"
            active={props.activeMetrics.includes("sessions")}
            onClick={() => props.onChangeMetric("sessions")}
          />
          <Metric label="Avg. Duration" value={duration || "0s"} />
          <Metric
            label="Events"
            activeClassName="bg-[#374151]"
            value={metrics?.events.toString() || "0"}
            active={props.activeMetrics.includes("events")}
            onClick={() => props.onChangeMetric("events")}
          />
        </>
      )}
    </div>
  );
}
