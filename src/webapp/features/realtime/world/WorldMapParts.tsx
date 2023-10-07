import PartsJson from "./worldmapparts.json";

const paths = PartsJson as { [key: string]: string };

type Props = {
  activeCountry?: string;
};

export function WorldMapParts(props: Props) {
  return (
    <g>
      {Object.entries(paths).map(([id, d]) => (
        <path key={id} id={id} d={d} data-active={props.activeCountry === id} />
      ))}
    </g>
  );
}
