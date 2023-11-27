import { Chart } from "@components/Chart";
import { useChartColors } from "@features/theme";
import { ChartConfigurationCustomTypesPerDataset } from "chart.js";

type Props = {
  dates: string[];
  events: number[];
  quota: number;
  state: "OK" | "OVERUSE";
};

export function MonthlyUsageChart(props: Props) {
  const colors = useChartColors();

  const config: ChartConfigurationCustomTypesPerDataset = {
    data: {
      labels: props.dates,
      datasets: [
        {
          type: "bar",
          label: "Daily Users",
          data: props.events,
          backgroundColor: colors.primary,
          hoverBackgroundColor: colors.primary,
        },
        {
          type: "line",
          data: props.events.map(() => props.quota),
          borderColor: props.state === "OK" ? colors.success : colors.destructive,
          borderWidth: 2,
          pointRadius: 0,
          pointHoverRadius: 0,
        },
      ],
    },
    options: {
      plugins: {
        tooltip: {
          enabled: false,
        },
      },
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
          border: {
            display: false,
          },
        },
        y: {
          grid: {
            color: colors.highlight,
            tickWidth: 0,
          },
          beginAtZero: true,
          ticks: {
            maxTicksLimit: 8,
          },
          border: {
            display: false,
          },
        },
      },
    },
  };

  return <Chart config={config} />;
}
