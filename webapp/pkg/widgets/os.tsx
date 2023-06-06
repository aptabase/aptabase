const fallbackImageUrl = new URL(`./icons/os/default.svg`, import.meta.url);

export function getOperatingSystemImageUrl(name: string) {
  const lcName = name.toLowerCase().replaceAll(/[^a-z]*/g, "");
  const svg = new URL(`./icons/os/${lcName}.svg`, import.meta.url);
  if (svg.pathname !== "/undefined") {
    return svg.href;
  }

  return fallbackImageUrl.href;
}
