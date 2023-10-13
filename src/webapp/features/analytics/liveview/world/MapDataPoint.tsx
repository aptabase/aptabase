import { useState, type MouseEvent } from "react";
import { WorldMapPopup } from "./WorldMapPopup";
import { projectAbsolute } from "./robinson";
import { CountryFlag, CountryName } from "@features/geo";

type Props = {
  regionName: string;
  countryCode: string;
  latitude: number;
  longitude: number;
  radius: number;
  users: number;
  onMouseEnter: (countryCode: string) => void;
  onMouseLeave: () => void;
};

type TooltipPosition = {
  x: number;
  y: number;
};

export function MapDataPoint(props: Props) {
  const [tooltip, setTooltip] = useState<TooltipPosition | undefined>(undefined);

  const mouseEnter = (evt: MouseEvent) => {
    if (tooltip) return;

    setTooltip({ x: evt.clientX, y: evt.clientY });
    props.onMouseEnter(props.countryCode);
  };

  const mouseLeave = () => {
    setTooltip(undefined);
    props.onMouseLeave();
  };

  const position =
    props.latitude < 0 && props.longitude <= -10
      ? "bottom-left"
      : props.latitude < 0 && props.longitude >= -10
      ? "bottom-right"
      : props.latitude > 0 && props.longitude <= -10
      ? "top-left"
      : "top-right";

  const [offsetX, offsetY] = {
    "bottom-left": [-20, 0],
    "bottom-right": [0, 0],
    "top-left": [-25, 10],
    "top-right": [-15, 10],
  }[position];

  const { x, y } = projectAbsolute(props.latitude, props.longitude, 2000, 1, offsetX, offsetY);
  return (
    <>
      <circle
        cx={x}
        cy={y}
        r={props.radius}
        onMouseEnter={mouseEnter}
        onMouseLeave={mouseLeave}
        className="text-success opacity-50"
      />

      {tooltip && (
        <WorldMapPopup {...tooltip}>
          <div className="p-2 text-sm">
            <span className="font-bold">{props.users}</span> recent users
            <div className="flex gap-1 items-center justify-between">
              <div>
                {props.regionName && <span>{props.regionName} ·</span>} <CountryName countryCode={props.countryCode} />
              </div>
              <CountryFlag countryCode={props.countryCode} size="sm" />
            </div>
          </div>
        </WorldMapPopup>
      )}
    </>
  );
}
