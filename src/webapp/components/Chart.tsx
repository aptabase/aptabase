import {
  BarController,
  BarElement,
  CategoryScale,
  ChartConfiguration,
  ChartConfigurationCustomTypesPerDataset,
  Chart as ChartJS,
  Filler,
  Legend,
  LinearScale,
  LineController,
  LineElement,
  PointElement,
  Tooltip,
} from "chart.js";
import Annotation from "chartjs-plugin-annotation";

import { useEffect, useRef } from "react";

export { type ChartConfigurationCustomTypesPerDataset, type ScriptableContext } from "chart.js";
export { type LineAnnotationOptions } from "chartjs-plugin-annotation";

ChartJS.defaults.font.family = "'Inter var', 'Helvetica Neue', 'Helvetica', 'Arial', sans-serif";

ChartJS.register(
  BarController,
  BarElement,
  LineController,
  LineElement,
  PointElement,
  CategoryScale,
  LinearScale,
  Tooltip,
  Filler,
  Annotation,
  Legend
);

type Props = {
  config: ChartConfiguration | ChartConfigurationCustomTypesPerDataset;
};

export function Chart(props: Props) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  let chartRef: ChartJS | undefined;

  useEffect(() => {
    if (!canvasRef.current) return;

    chartRef = new ChartJS(canvasRef.current, props.config);
    return () => chartRef?.destroy();
  }, [canvasRef, props.config]);

  return <canvas ref={canvasRef} />;
}
