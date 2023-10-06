import { projectAbsolute } from "./robinson";

type Props = {
  lat: number;
  lng: number;
  size: number;
};

export function MapDataPoint(props: Props) {
  const mapWidth = 2000;
  const heightFactor = 1;
  const offsetX = -30;
  const offsetY = 0;

  const { x, y } = projectAbsolute(props.lat, props.lng, mapWidth, heightFactor, offsetX, offsetY);
  return <circle cx={x} cy={y} r={props.size} className="text-success opacity-80" />;
}
