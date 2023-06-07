import { IconApple } from "./apple";

type Props = {
  name: string;
  className: string;
};

const fallbackImageUrl = new URL(`./default.svg`, import.meta.url);

function getOperatingSystemImageUrl(name: string) {
  const svg = new URL(`./${name}.svg`, import.meta.url);
  if (svg.pathname !== "/undefined") {
    return svg.href;
  }

  return fallbackImageUrl.href;
}

export function OSIcon(props: Props) {
  let lcName = props.name.toLowerCase().replaceAll(/[^a-z]*/g, "");
  if (lcName === "ios" || lcName === "ipados") {
    return <IconApple className={props.className} />;
  }

  return (
    <img src={getOperatingSystemImageUrl(lcName)} className={props.className} />
  );
}
