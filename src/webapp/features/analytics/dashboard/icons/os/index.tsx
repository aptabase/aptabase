import { Icon3dCubeSphere } from "@tabler/icons-react";
import { IconApple } from "./apple";

type Props = {
  name: string;
  className: string;
};

function getOperatingSystemImageUrl(name: string): string | undefined {
  const svg = new URL(`./${name}.svg`, import.meta.url);
  if (svg.href.endsWith("/undefined")) {
    return undefined;
  }

  return svg.href;
}

export function OSIcon(props: Props) {
  let lcName = props.name.toLowerCase().replaceAll(/[^a-z]*/g, "");
  if (lcName === "ios" || lcName === "ipados" || lcName === "tvos") {
    return <IconApple className={props.className} />;
  }

  const imageUrl = getOperatingSystemImageUrl(lcName);
  if (!imageUrl) {
    return <Icon3dCubeSphere className={props.className} />;
  }

  return <img src={imageUrl} className={props.className} loading="lazy" />;
}
