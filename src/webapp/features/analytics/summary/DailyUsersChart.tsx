import { Chart } from "@components/Chart";
import { ChartConfigurationCustomTypesPerDataset } from "chart.js";
import colors from "../key_metrics/colors";

type Props = {
  values: number[];
};

export function DailyUsersChart(props: Props) {
  const scales = {
    grid: {
      display: false,
    },
    ticks: {
      display: false,
    },
    border: {
      display: false,
    },
  };

  const config: ChartConfigurationCustomTypesPerDataset = {
    data: {
      labels: props.values,
      datasets: [
        {
          type: "line",
          label: "Daily Users",
          data: props.values,
          borderColor: colors.primary,
          fill: "origin",
          backgroundColor: colors.primaryBackground,
          tension: 0.05,
          borderWidth: 1.5,
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
        padding: {
          left: -8,
          bottom: -8,
        },
      },
      scales: {
        x: {
          ...scales,
        },
        y: {
          beginAtZero: true,
          ...scales,
        },
      },
    },
  };

  return <Chart config={config} />;
}
