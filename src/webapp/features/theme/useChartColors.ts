import { useTheme } from "./ThemeProvider";
import { useMemo } from "react";

function createLinearGradient(color1: string, color2: string, color3: string): CanvasGradient | string {
  const canvas = document.createElement("canvas");
  const context = canvas.getContext("2d");

  if (!context) return color1;

  var gradient = context.createLinearGradient(0, 0, 0, 400);
  gradient.addColorStop(0, color1);
  gradient.addColorStop(0.5, color2);
  gradient.addColorStop(1, color3);

  return gradient;
}

type ChartColors = {
  primary: string;
  primaryBackground: CanvasGradient | string;
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
    primaryBackground: createLinearGradient("rgba(96, 165, 250, 0.2)", "rgba(96, 165, 250, 0.1)", "transparent"),
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
