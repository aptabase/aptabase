import { useState, type MouseEvent } from "react";
import { WorldMapPopup } from "./WorldMapPopup";
import { projectAbsolute } from "./robinson";
import { CountryFlag, CountryName } from "@features/geo";

type Props = {
  region: string;
  countryCode: string;
  lat: number;
  lng: number;
  size: number;
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

  const { x, y } = projectAbsolute(props.lat, props.lng, 2000, 0.95, -10, 0);
  return (
    <>
      <circle
        cx={x}
        cy={y}
        r={props.size}
        onMouseEnter={mouseEnter}
        onMouseLeave={mouseLeave}
        className="text-success opacity-50"
      />

      {tooltip && (
        <WorldMapPopup {...tooltip}>
          <div className="p-2 text-sm">
            <span className="font-bold">5</span> recent users
            <div className="flex gap-1 items-center justify-between">
              <div>
                {props.region && <span>{props.region} ·</span>} <CountryName countryCode={props.countryCode} />
              </div>
              <CountryFlag countryCode={props.countryCode} size="sm" />
            </div>
          </div>
        </WorldMapPopup>
      )}
    </>
  );
}
