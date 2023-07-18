function createLinearGradient(
  color1: string,
  color2: string,
  color3: string
): CanvasGradient | string {
  const canvas = document.createElement("canvas");
  const context = canvas.getContext("2d");

  if (!context) return color1;

  var gradient = context.createLinearGradient(0, 0, 0, 400);
  gradient.addColorStop(0, color1);
  gradient.addColorStop(0.2, color2);
  gradient.addColorStop(1, color3);

  return gradient;
}

// https://stackoverflow.com/questions/28569667/fill-chart-js-bar-chart-with-diagonal-stripes-or-other-patterns
function createDiagonalPattern(color: string): CanvasPattern | string {
  const canvas = document.createElement("canvas");
  const context = canvas.getContext("2d");

  if (!context) return color;

  const size = 8;
  const stroke = 2;
  const strokeOffset = stroke / 2;
  canvas.width = size;
  canvas.height = size;

  context.strokeStyle = color;
  context.lineWidth = stroke;

  context.moveTo(size / 2 - strokeOffset, -strokeOffset);
  context.lineTo(size + strokeOffset, size / 2 + strokeOffset);
  context.moveTo(-strokeOffset, size / 2 - strokeOffset);
  context.lineTo(size / 2 + strokeOffset, size + strokeOffset);
  context.stroke();

  return context.createPattern(canvas, "repeat") || color;
}

const root = getComputedStyle(document.documentElement);

const colors = {
  primary: createLinearGradient("#3b82f6", "#3b82f6", "#bfdbfe"),
  primaryStripped: createDiagonalPattern("#3b82f6"),
  secondary: `hsl(${root.getPropertyValue("--foreground")})`,
  highlight: root.getPropertyValue("--chart-highlight"),
};

export default colors;
