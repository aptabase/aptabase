import { IconBox } from "@tabler/icons-react";

type Props = {
  iconPath: string;
  className: string;
};

export function AppIcon(props: Props) {
  if (props.iconPath) {
    return (
      <img
        src={`/uploads/${props.iconPath}`}
        className={`${props.className} rounded`}
        loading="lazy"
        alt="App Icon"
      />
    );
  }

  return <IconBox strokeWidth={1.75} className={`${props.className} p-0.5 border rounded`} />;
}
