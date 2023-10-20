import { useState } from "react";
import { MapDataPoint } from "./MapDataPoint";
import { WorldMapParts } from "./WorldMapParts";

type Point = {
  users: number;
  countryCode: string;
  regionName: string;
  latitude: number;
  longitude: number;
};

type Props = {
  className?: string;
  points: Point[];
};

function calculateRadius(users: number, maxUsers: number) {
  const minRadius = 5;
  const maxRadius = 20;
  const scaleFactor = users / (maxUsers || 1);
  return minRadius + scaleFactor * (maxRadius - minRadius);
}

export function WorldMap(props: Props) {
  const [activeCountry, setActiveCountry] = useState<string | undefined>(undefined);
  const maxUsers = props.points.reduce((max, point) => Math.max(max, point.users), 0);

  return (
    <>
      <svg
        id="worldmap"
        baseProfile="tiny"
        className={`text-foreground stroke-background ${props.className}`}
        fill="currentColor"
        version="1.2"
        strokeWidth="1"
        viewBox="0 0 2000 857"
        xmlns="http://www.w3.org/2000/svg"
      >
        <style
          dangerouslySetInnerHTML={{
            __html: `
            #worldmap path { opacity: 0.15 }
            #worldmap path:hover, #worldmap path[data-active="true"] { opacity: 0.4 }`,
          }}
        />

        <WorldMapParts activeCountry={activeCountry} />

        {props.points.map((point) => (
          <MapDataPoint
            key={`${point.latitude}-${point.longitude}`}
            {...point}
            radius={calculateRadius(point.users, maxUsers)}
            onMouseEnter={setActiveCountry}
            onMouseLeave={() => setActiveCountry(undefined)}
          />
        ))}
      </svg>
      <div id="world-map-tooltip-container" />
    </>
  );
}
