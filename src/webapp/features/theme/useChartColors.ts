import { Chart } from "chart.js";
import { useTheme } from "./ThemeProvider";
import { useMemo } from "react";

function createLinearGradient(chart: Chart, color1: string, color2: string): CanvasGradient | string {
  const gradient = chart.ctx.createLinearGradient(0, 0, 0, chart.height);
  gradient.addColorStop(0, color1);
  gradient.addColorStop(1, color2);
  return gradient;
}

type ChartColors = {
  primary: string;
  primaryBackground: (chart: Chart) => CanvasGradient | string;
  success: string;
  destructive: string;
  warning: string;
  background: string;
  highlight: string;
};

const getColors = (): ChartColors => {
  const root = getComputedStyle(document.body);
  return {
    primary: "#60A5FA",
    primaryBackground: (chart: Chart) =>
      createLinearGradient(chart, "rgba(96, 165, 250, 0.2)", "rgba(96, 165, 250, 0)"),
    success: `hsl(${root.getPropertyValue("--success")})`,
    destructive: `hsl(${root.getPropertyValue("--destructive")})`,
    warning: `hsl(${root.getPropertyValue("--warning")})`,
    background: `hsl(${root.getPropertyValue("--background")})`,
    highlight: root.getPropertyValue("--chart-highlight"),
  };
};

export function useChartColors(): ChartColors {
  const { theme } = useTheme();
  return useMemo(getColors, [theme]);
}
