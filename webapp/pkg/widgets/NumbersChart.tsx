import {
  LineController,
  LineElement,
  PointElement,
  CategoryScale,
  Chart,
  LinearScale,
  Tooltip,
  Filler,
} from "chart.js";
import { useEffect, useRef } from "react";

Chart.defaults.font.family =
  "'Inter var', 'Helvetica Neue', 'Helvetica', 'Arial', sans-serif";

Chart.register(
  LineController,
  LineElement,
  PointElement,
  CategoryScale,
  LinearScale,
  Tooltip,
  Filler
);

type Props = {
  values: number[];
};

export function NumbersChart(props: Props) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const chartRef = useRef<Chart | null>(null);

  useEffect(() => {
    if (!canvasRef.current) return;

    chartRef.current = new Chart(canvasRef.current, {
      data: {
        labels: props.values,
        datasets: [
          {
            type: "line",
            label: "Clicks",
            data: props.values,
            borderColor: "#3b82f6",
            tension: 0.2,
            fill: "origin",
            backgroundColor: "rgb(59, 130, 246, 0.1)",
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
          padding: {
            left: -8,
            bottom: -8,
          },
        },
        scales: {
          x: {
            grid: {
              display: false,
            },
            ticks: {
              display: false,
            },
            border: {
              display: false,
            },
          },
          y: {
            beginAtZero: true,
            grid: {
              display: false,
            },
            ticks: {
              display: false,
            },
            border: {
              display: false,
            },
          },
        },
      },
    });

    return () => chartRef.current?.destroy();
  }, [canvasRef, props.values]);

  return <canvas ref={canvasRef}></canvas>;
}
