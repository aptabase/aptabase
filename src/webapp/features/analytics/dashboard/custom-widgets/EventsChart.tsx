import { LineChart } from "@features/analytics/dashboard/LineChart";
import { PeriodicStats } from "@features/analytics/query";
import { useChartColors } from "@features/theme";
import { QueryObserverResult, RefetchOptions } from "@tanstack/react-query";
import { useMemo } from "react";

export type EventSeries = {
  name: string;
  values: number[];
};

type TooltipDataPoint = {
  label: string;
  points: Array<{
    name: string;
    value: number;
  }>;
};

type Props = {
  hasPartialData: boolean;
  labels: string[];
  series: EventSeries[];
  granularity: "hour" | "day" | "month";
  isEmpty?: boolean;
  isLoading?: boolean;
  isError?: boolean;
  formatLabel?: (label: string | number) => string;
  renderTooltip?: (dataPoint: TooltipDataPoint) => JSX.Element;
  refetch?: (options?: RefetchOptions | undefined) => Promise<QueryObserverResult<PeriodicStats[], Error>>;
};

export function EventsChart(props: Props) {
  const colors = useChartColors();

  const datasets = useMemo(() => {
    return props.series.map((event, index) => ({
      label: event.name,
      data: event.values,
      hasPartialData: props.hasPartialData,
      color: colors.series[index],
    }));
  }, [props.series, props.hasPartialData, colors.series]);

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
