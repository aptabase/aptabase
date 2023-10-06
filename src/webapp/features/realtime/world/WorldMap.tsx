import { MapDataPoint } from "./MapDataPoint";
import { WorldMapParts } from "./WorldMapParts";

type Point = {
  lat: number;
  lng: number;
  size: number;
  countryCode: string;
  region: string;
};

type Props = {
  className?: string;
  points: Point[];
};

export function WorldMap(props: Props) {
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
          #worldmap path { opacity: 0.1 }
          #worldmap path:hover { opacity: 0.2 }`,
          }}
        />

        <WorldMapParts />

        {props.points.map((point) => (
          <MapDataPoint key={`${point.lat}-${point.lng}`} {...point} />
        ))}
      </svg>
      <div id="world-map-tooltip-container" />
    </>
  );
}
