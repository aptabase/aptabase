import { Metric } from "./charts";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { keyMetrics } from "./query";
import { useApps } from "../apps";

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

  return (
    <div className="h-24 flex justify-between sm:justify-start sm:space-x-4 mb-2">
      {!isLoading && !isError && (
        <>
          <Metric
            label="Sessions"
            current={metrics?.current.sessions ?? 0}
            previous={metrics?.previous?.sessions}
            activeClassName="bg-primary"
            active={props.activeMetrics.includes("sessions")}
            onClick={() => props.onChangeMetric("sessions")}
            format="number"
          />
          <Metric
            label="Events"
            activeClassName="bg-foreground"
            current={metrics?.current.events ?? 0}
            previous={metrics?.previous?.events}
            active={props.activeMetrics.includes("events")}
            onClick={() => props.onChangeMetric("events")}
            format="number"
          />
          <Metric
            label="Avg. Duration"
            current={metrics?.current.durationSeconds || 0}
            previous={metrics?.previous?.durationSeconds}
            format="duration"
          />
        </>
      )}
    </div>
  );
}
