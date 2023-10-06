import { MapDataPoint } from "./MapDataPoint";
import { WorldMapParts } from "./WorldMapParts";

type Point = {
  lat: number;
  lng: number;
  size: number;
};

type Props = {
  className?: string;
  points: Point[];
};

export function WorldMap(props: Props) {
  return (
    <svg
      baseProfile="tiny"
      className={`text-border ${props.className}`}
      fill="currentColor"
      version="1.2"
      viewBox="0 0 2000 857"
      xmlns="http://www.w3.org/2000/svg"
    >
      <WorldMapParts />

      {props.points.map((point) => (
        <MapDataPoint key={`${point.lat}-${point.lng}`} lat={point.lat} lng={point.lng} size={point.size} />
      ))}
    </svg>
  );
}
