import { OSIcon } from "./icons/os";

type Props = {
  name: string;
};

export function OS(props: Props) {
  return (
    <span className="flex items-center space-x-2">
      <OSIcon name={props.name} className="h-5 w-5" />
      <p>{props.name || "Unknown"}</p>
    </span>
  );
}
