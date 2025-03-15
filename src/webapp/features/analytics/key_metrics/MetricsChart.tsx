import { LineChart } from "@features/analytics/dashboard/LineChart";
import { useChartColors } from "@features/theme";
import { QueryObserverResult, RefetchOptions } from "@tanstack/react-query";
import { useMemo } from "react";
import { PeriodicStats } from "../query";

type Props = {
  hasPartialData: boolean;
  labels: string[];
  activeMetric: "users" | "sessions" | "events";
  users: number[];
  sessions: number[];
  events: number[];
  granularity: "hour" | "day" | "month";
  isEmpty?: boolean;
  isLoading?: boolean;
  isError?: boolean;
  formatLabel?: (label: string | number) => string;
  renderTooltip?: (dataPoint: TooltipDataPoint) => JSX.Element;
  refetch?: (options?: RefetchOptions | undefined) => Promise<QueryObserverResult<PeriodicStats, Error>>;
};

type TooltipDataPoint = {
  label: string;
  points: Array<{
    name: string;
    value: number;
  }>;
};

const labels = {
  "sessions-hour": "Sessions",
  "sessions-day": "Sessions",
  "sessions-month": "Sessions",
  "events-hour": "Events",
  "events-day": "Events",
  "events-month": "Events",
  "users-hour": "Users",
  "users-day": "Users",
  "users-month": "Daily Users",
};

export function MetricsChart(props: Props) {
  const colors = useChartColors();
  const label = labels[`${props.activeMetric}-${props.granularity}`] ?? "";

  const datasets = useMemo(() => {
    return [
      {
        label,
        data: props[props.activeMetric],
        hasPartialData: props.hasPartialData,
        color: colors.primary,
      },
    ];
  }, [props.activeMetric, props.hasPartialData, props[props.activeMetric], colors.primary, label]);

  return (
    <LineChart
      labels={props.labels}
      datasets={datasets}
      granularity={props.granularity}
      isEmpty={props.isEmpty}
      isLoading={props.isLoading}
      isError={props.isError}
      formatLabel={props.formatLabel}
      renderTooltip={props.renderTooltip}
      refetch={props.refetch}
    />
  );
}
