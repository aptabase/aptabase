import { createPortal } from "react-dom";

type Props = {
  x: number;
  y: number;
  children: React.ReactNode;
};

export function WorldMapPopup(props: Props) {
  const container = document.getElementById("world-map-tooltip-container");
  if (!container) return null;

  return createPortal(
    <div style={{ top: props.y, left: props.x + 10 }} className="absolute rounded border bg-background">
      {props.children}
    </div>,
    container
  );
}
