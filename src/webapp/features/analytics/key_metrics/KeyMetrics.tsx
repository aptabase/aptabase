import { Metric } from "./Metric";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { keyMetrics } from "../query";
import { useApps } from "@features/apps";
import { KeyMetricsContainer } from "./MetricsContainer";
import { useDatePicker } from "@hooks/use-datepicker";

type Props = {
  appId: string;
  activeMetric: "users" | "sessions" | "events";
  onChangeActiveMetric: (metric: "users" | "sessions" | "events") => void;
};

export function KeyMetrics(props: Props) {
  const { buildMode } = useApps();
  const [searchParams] = useSearchParams();
  const { startDate, endDate } = useDatePicker();
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const {
    isLoading,
    isError,
    data: metrics,
  } = useQuery({
    queryKey: ["key-metrics", buildMode, props.appId, startDate, endDate, countryCode, appVersion, eventName, osName],
    queryFn: () =>
      keyMetrics({
        buildMode,
        appId: props.appId,
        startDate,
        endDate,
        countryCode,
        appVersion,
        eventName,
        osName,
      }),
    staleTime: 10000,
  });

  return (
    <KeyMetricsContainer>
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
            current={metrics?.current.events ?? 0}
            previous={metrics?.previous?.events}
            activeClassName="bg-primary"
            active={props.activeMetric === "events"}
            onClick={() => props.onChangeActiveMetric("events")}
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
    </KeyMetricsContainer>
  );
}
