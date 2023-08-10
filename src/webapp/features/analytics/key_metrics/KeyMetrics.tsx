import { Metric } from "./Metric";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { keyMetrics } from "../query";
import { useApps } from "@features/apps";

type Props = {
  appId: string;
  activeMetric: "users" | "sessions";
  onChangeActiveMetric: (metric: "users" | "sessions") => void;
  showEvents: boolean;
  onToggleShowEvents: () => void;
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
    ["key-metrics", buildMode, props.appId, period, countryCode, appVersion, eventName, osName],
    () =>
      keyMetrics({
        buildMode,
        appId: props.appId,
        period,
        countryCode,
        appVersion,
        eventName,
        osName,
      }),
    { staleTime: 10000 }
  );

  return (
    <div className="mb-10 grid grid-cols-2 gap-4 sm:flex sm:h-22 sm:justify-start sm:gap-8">
      {!isLoading && !isError && (
        <>
          <Metric
            label="Daily Users"
            current={metrics?.current.dailyUsers ?? 0}
            previous={metrics?.previous?.dailyUsers}
            activeClassName="bg-primary"
            active={props.activeMetric === "users"}
            onClick={() => props.onChangeActiveMetric("users")}
            format="number"
          />
          <Metric
            label="Sessions"
            current={metrics?.current.sessions ?? 0}
            previous={metrics?.previous?.sessions}
            activeClassName="bg-primary"
            active={props.activeMetric === "sessions"}
            onClick={() => props.onChangeActiveMetric("sessions")}
            format="number"
          />
          <Metric
            label="Events"
            activeClassName="bg-foreground"
            current={metrics?.current.events ?? 0}
            previous={metrics?.previous?.events}
            active={props.showEvents}
            onClick={props.onToggleShowEvents}
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
