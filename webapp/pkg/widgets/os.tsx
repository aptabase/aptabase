const fallbackImageUrl = new URL(`./icons/os/default.svg`, import.meta.url).href;

export function getOperatingSystemImageUrl(name: string) {
  const lcName = name.toLowerCase().replaceAll(/[^a-z]*/g, "");
  const png = new URL(`./icons/os/${lcName}.png`, import.meta.url);
  if (png.pathname !== "/undefined") {
    return png.href;
  }

  const svg = new URL(`./icons/os/${lcName}.svg`, import.meta.url);
  if (svg.pathname !== "/undefined") {
    return svg.href;
  }

  return fallbackImageUrl;
}
