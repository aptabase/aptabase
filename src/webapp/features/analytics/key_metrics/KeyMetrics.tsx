import { useApps } from "@features/apps";
import { useQuery } from "@tanstack/react-query";
import { useAtomValue } from "jotai";
import { useSearchParams } from "react-router-dom";
import { dateFilterValuesAtom } from "../../../atoms/date-atoms";
import { keyMetrics } from "../query";
import { Metric } from "./Metric";
import { KeyMetricsContainer } from "./MetricsContainer";

type Props = {
  appId: string;
  activeMetric: "users" | "sessions" | "events";
  onChangeActiveMetric: (metric: "users" | "sessions" | "events") => void;
};

export function KeyMetrics(props: Props) {
  const { buildMode } = useApps();
  const [searchParams] = useSearchParams();
  const { startDateIso, endDateIso, granularity } = useAtomValue(dateFilterValuesAtom);
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const {
    isLoading,
    isError,
    data: metrics,
  } = useQuery({
    queryKey: [
      "key-metrics",
      buildMode,
      props.appId,
      startDateIso,
      endDateIso,
      countryCode,
      appVersion,
      eventName,
      osName,
    ],
    queryFn: () =>
      keyMetrics({
        buildMode,
        appId: props.appId,
        startDate: startDateIso,
        endDate: endDateIso,
        granularity,
        countryCode,
        appVersion,
        eventName,
        osName,
      }),
    staleTime: 10000,
    enabled: !!startDateIso && !!endDateIso && !!granularity,
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
