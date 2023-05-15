import { EmptyState, ErrorState, LoadingState } from "@app/primitives";
import {
  BarController,
  LineController,
  BarElement,
  LineElement,
  PointElement,
  CategoryScale,
  Chart,
  LinearScale,
  Tooltip,
  ScriptableContext,
} from "chart.js";
import Annotation, { LineAnnotationOptions } from "chartjs-plugin-annotation";
import { useEffect, useRef, useState } from "react";
import colors from "./colors";

Chart.defaults.font.family =
  "'Inter var', 'Helvetica Neue', 'Helvetica', 'Arial', sans-serif";

Chart.register(
  BarController,
  BarElement,
  LineController,
  LineElement,
  PointElement,
  CategoryScale,
  LinearScale,
  Tooltip,
  Annotation
);

type Props = {
  hasPartialData: boolean;
  metrics: string[];
  labels: string[];
  sessions: number[];
  events: number[];
  showAllLabels: boolean;
  isEmpty?: boolean;
  isLoading?: boolean;
  isError?: boolean;
  formatLabel: (label: string | number) => string;
  renderTooltip: (dataPoint: TooltipDataPoint) => JSX.Element;
};

type TooltipDataPoint = {
  label: string;
  points: Array<{
    name: string;
    value: number;
  }>;
};

export function MetricsChart(props: Props) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const tooltipRef = useRef<HTMLDivElement>(null);
  const [tooltipDataPoint, setTooltipDataPoint] =
    useState<TooltipDataPoint | null>(null);
  let chartInstance: Chart;

  useEffect(() => {
    if (!canvasRef.current) return;

    chartInstance = new Chart(canvasRef.current, {
      data: {
        labels: props.labels,
        datasets: [
          {
            order: 2,
            type: "bar",
            label: "Sessions",
            yAxisID: "sessions",
            hidden: !props.metrics.includes("sessions"),
            data: props.sessions,
            backgroundColor: props.hasPartialData
              ? (ctx: ScriptableContext<"bar">) => {
                  const total = ctx.chart.data.datasets[0].data.length;
                  return total - 1 === ctx.dataIndex
                    ? colors.primaryStripped
                    : colors.primary;
                }
              : colors.primary,
            borderRadius: 2,
          },
          {
            order: 1,
            type: "line",
            label: "Events",
            data: props.events,
            yAxisID: "events",
            hidden: !props.metrics.includes("events"),
            borderColor: "#374151",
            tension: 0.1,
            pointRadius: 0,
            pointHoverRadius: 0,
            segment: {
              borderDash: props.hasPartialData
                ? (ctx) => {
                    const total =
                      chartInstance?.data.datasets[0].data.length || 0;
                    return total - 1 === ctx.p1DataIndex ? [8, 8] : [];
                  }
                : [],
            },
          },
        ],
      },
      options: {
        animation: {
          duration: 200,
          easing: "linear",
        },
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
              maxRotation: props.showAllLabels ? 45 : 0,
              autoSkipPadding: props.showAllLabels ? 0 : 20,
              maxTicksLimit: props.showAllLabels ? 0 : 8,
              callback: function (value) {
                const label =
                  typeof value === "number"
                    ? this.getLabelForValue(value)
                    : value;
                return props.formatLabel(label);
              },
            },
            border: {
              display: false,
            },
          },
          sessions: {
            display: props.metrics.includes("sessions"),
            grid: {
              tickWidth: 0,
            },
            beginAtZero: true,
            ticks: {
              maxTicksLimit: 8,
            },
            border: {
              display: false,
              dash: [4, 4],
            },
          },
          events: {
            display: props.metrics.includes("events"),
            position: props.metrics.length === 1 ? "left" : "right",
            grid: {
              tickWidth: 0,
            },
            beginAtZero: true,
            ticks: {
              maxTicksLimit: 8,
            },
            border: {
              display: false,
              dash: [4, 4],
            },
          },
        },
        plugins: {
          annotation: {
            annotations: [
              {
                type: "line",
                display: false,
                borderColor: colors.highlight,
                borderWidth: (ctx) => {
                  const columns = ctx.chart.data.datasets[0].data.length;
                  const chartWidth = ctx.chart.width + 100;
                  return chartWidth / columns;
                },
              },
            ],
          },
          tooltip: {
            enabled: false,
            position: "nearest",

            external: function ({ tooltip, chart }) {
              if (!tooltipRef.current) return;

              const annotations = chartInstance.options.plugins?.annotation
                ?.annotations as LineAnnotationOptions[];
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

              var offsetY = Math.min(
                ...tooltip.dataPoints.map(
                  (x) => x.element.tooltipPosition(true).y
                )
              );

              const { offsetLeft: positionX } = chart.canvas;
              tooltipRef.current.style.left = positionX + tooltip.caretX + "px";
              tooltipRef.current.style.top = offsetY + 100 + "px";
              tooltipRef.current.style.padding =
                tooltip.options.padding +
                "px " +
                tooltip.options.padding +
                "px";
            },
          },
        },
      },
    });

    return () => chartInstance.destroy();
  }, [canvasRef, props]);

  return (
    <div className="h-80 w-full">
      {props.isError ? (
        <ErrorState />
      ) : props.isLoading ? (
        <LoadingState />
      ) : props.isEmpty ? (
        <EmptyState />
      ) : (
        <>
          <canvas ref={canvasRef}></canvas>
          <div
            ref={tooltipRef}
            style={{ opacity: 0 }}
            className="absolute pointer-events-none shadow border border-default bg-white p-2 rounded transform -translate-x-1/2"
          >
            {tooltipDataPoint ? props.renderTooltip(tooltipDataPoint) : null}
          </div>
        </>
      )}
    </div>
  );
}
