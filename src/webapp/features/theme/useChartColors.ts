import { Chart } from "chart.js";
import { useMemo } from "react";
import { useTheme } from "./ThemeProvider";

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
  series: string[];
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
    series: [
      "#60A5FA", // blue-400
      "#34D399", // emerald-400
      "#818CF8", // indigo-400
      "#A8A29E", // stone-400 (gray)
      "#C084FC", // purple-400
      "#FCD34D", // amber-300 (yellow)
      "#A78BFA", // violet-400
      "#F97316", // orange-500 (muted)
      "#64748B", // slate-500 (darker gray)
      "#94A3B8", // slate-400 (blue-gray)
    ],
  };
};

export function useChartColors(): ChartColors {
  const { theme } = useTheme();
  return useMemo(getColors, [theme]);
}
