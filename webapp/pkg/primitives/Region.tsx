import { region } from "@app/env";

const flags: { [host: string]: string } = {
  us: new URL(`./icons/us.svg`, import.meta.url).href,
  eu: new URL(`./icons/eu.svg`, import.meta.url).href,
};

export function RegionFlag() {
  if (!flags[region]) return null;

  return <img className="w-5 h-5 shadow rounded-full" src={flags[region]} />;
}
