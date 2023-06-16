import { Icon3dCubeSphere, IconCube } from "@tabler/icons-react";
import { IconApple } from "./apple";

type Props = {
  name: string;
  className: string;
};

const fallbackImageUrl = new URL(`./default.svg`, import.meta.url);

function getOperatingSystemImageUrl(name: string): string | undefined {
  const svg = new URL(`./${name}.svg`, import.meta.url);
  if (svg.pathname !== "/undefined") {
    return svg.href;
  }

  return undefined;
}

export function OSIcon(props: Props) {
  let lcName = props.name.toLowerCase().replaceAll(/[^a-z]*/g, "");
  if (lcName === "ios" || lcName === "ipados") {
    return <IconApple className={props.className} />;
  }

  const imageUrl = getOperatingSystemImageUrl(lcName);
  if (imageUrl) {
    return <Icon3dCubeSphere className={props.className} />;
  }

  return <img src={imageUrl} className={props.className} />;
}
