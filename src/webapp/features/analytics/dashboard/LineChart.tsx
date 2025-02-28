import { Chart, type ChartConfigurationCustomTypesPerDataset, type LineAnnotationOptions } from "@components/Chart";
import { EmptyState } from "@components/EmptyState";
import { ErrorState } from "@components/ErrorState";
import { LoadingState } from "@components/LoadingState";
import { useChartColors } from "@features/theme";
import { QueryObserverResult, RefetchOptions } from "@tanstack/react-query";
import { useMemo, useRef, useState } from "react";

type Dataset = {
  label: string;
  data: number[];
  color?: string;
  hasPartialData?: boolean;
};

type TooltipDataPoint = {
  label: string;
  points: Array<{
    name: string;
    value: number;
  }>;
};

type Props = {
  labels: string[];
  datasets: Dataset[];
  granularity: "hour" | "day" | "month";
  isEmpty?: boolean;
  isLoading?: boolean;
  isError?: boolean;
  formatLabel?: (label: string | number) => string;
  renderTooltip?: (dataPoint: TooltipDataPoint) => JSX.Element;
  refetch?: (options?: RefetchOptions | undefined) => Promise<QueryObserverResult<unknown, Error>>;
};

export function LineChart(props: Props) {
  const tooltipRef = useRef<HTMLDivElement>(null);
  const [tooltipDataPoint, setTooltipDataPoint] = useState<TooltipDataPoint | null>(null);
  const colors = useChartColors();

  const showAllLabels = props.granularity === "month";

  const config: ChartConfigurationCustomTypesPerDataset = useMemo(() => {
    return {
      data: {
        labels: props.labels,
        datasets: props.datasets.map((dataset) => ({
          label: dataset.label,
          type: "line",
          fill: "origin",
          backgroundColor: (ctx) => colors.primaryBackground(ctx.chart),
          data: dataset.data,
          borderColor: dataset.color ?? colors.primary,
          segment: {
            borderDash: dataset.hasPartialData
              ? (ctx) => {
                  const total = dataset.data.length || 0;
                  return total - 1 === ctx.p1DataIndex ? [4, 4] : [];
                }
              : [],
          },
          tension: 0.05,
          borderWidth: 2,
          pointRadius: 0,
          pointBackgroundColor: dataset.color ?? colors.primary,
          pointHoverRadius: 6,
          pointHoverBorderWidth: 4,
          pointHoverBorderColor: colors.background,
        })),
      },
      options: {
        animation: false,
        interaction: {
          mode: "index",
          intersect: false,
        },
        maintainAspectRatio: false,
        layout: {
          padding: 0,
        },
        scales: {
          x: {
            grid: {
              display: false,
            },
            ticks: {
              autoSkip: true,
              maxRotation: showAllLabels ? 45 : 0,
              autoSkipPadding: showAllLabels ? 0 : 20,
              maxTicksLimit: showAllLabels ? 0 : 8,
              callback: function (value) {
                const label = typeof value === "number" ? this.getLabelForValue(value) : value;
                return props.formatLabel?.(label) ?? value;
              },
            },
            border: {
              display: false,
            },
          },
          y: {
            grid: {
              tickWidth: 0,
            },
            beginAtZero: true,
            ticks: {
              maxTicksLimit: 4,
            },
            border: {
              display: false,
              dash: [4, 4],
            },
          },
        },
        plugins: {
          legend: {
            display: props.datasets.length > 1,
            position: "top" as const,
            labels: {
              usePointStyle: true,
              pointStyle: "circle",
              boxWidth: 8,
              boxHeight: 8,
              padding: 12,
            },
          },
          annotation: {
            annotations: [
              {
                type: "line",
                display: false,
                borderColor: colors.highlight,
                borderWidth: 4,
              },
            ],
          },
          tooltip: {
            enabled: false,
            position: "nearest",
            external: function ({ tooltip, chart }) {
              if (!tooltipRef.current) return;

              const annotations = chart.options.plugins?.annotation?.annotations as LineAnnotationOptions[];
              const highlight = annotations[0];

              if (tooltip.opacity === 0) {
                tooltipRef.current.style.opacity = "0";
                highlight.display = false;
                chart.update("none");
                return;
              }

              const dataPoint = tooltip.dataPoints[0];
              const label = dataPoint.label;
              const points = tooltip.dataPoints.map((dataPoint) => ({
                name: dataPoint.dataset.label ?? "",
                value: dataPoint.parsed.y ?? 0,
              }));

              setTooltipDataPoint({ label, points });

              if (highlight && highlight.xMin !== dataPoint.dataIndex) {
                highlight.display = true;
                highlight.xMin = dataPoint.dataIndex;
                highlight.xMax = dataPoint.dataIndex;
                chart.update("none");
              }

              tooltipRef.current.style.opacity = "1";

              var offsetY = Math.min(...tooltip.dataPoints.map((x) => x.element.tooltipPosition(true).y));

              const isMobile = chart.height < 300;
              const topOffset = isMobile ? 230 : 100;

              const { offsetLeft: positionX } = chart.canvas;
              tooltipRef.current.style.left = positionX + tooltip.caretX + "px";
              tooltipRef.current.style.top = offsetY + topOffset + "px";
              tooltipRef.current.style.padding = tooltip.options.padding + "px " + tooltip.options.padding + "px";
            },
          },
        },
      },
    };
  }, [props, colors]);

  return (
    <div className="h-60 md:h-80 w-full">
      {props.isError ? (
        <ErrorState refetch={props.refetch} />
      ) : props.isLoading ? (
        <LoadingState />
      ) : props.isEmpty ? (
        <EmptyState />
      ) : (
        <>
          <Chart config={config} />
          <div
            ref={tooltipRef}
            style={{ opacity: 0 }}
            className="absolute pointer-events-none shadow border bg-card p-2 rounded transform -translate-x-1/2"
          >
            {tooltipDataPoint && props.renderTooltip ? props.renderTooltip(tooltipDataPoint) : null}
          </div>
        </>
      )}
    </div>
  );
}
