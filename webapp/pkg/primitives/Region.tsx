const regions: { [host: string]: "eu" | "us" } = {
  "api-us.aptabase.com": "us",
  "api-eu.aptabase.com": "eu",
  "us.aptabase.com": "us",
  "eu.aptabase.com": "eu",
};

export const region = regions[window.location.hostname] || "eu";

const flags: { [host: string]: string } = {
  us: new URL(`./icons/us.svg`, import.meta.url).href,
  eu: new URL(`./icons/eu.svg`, import.meta.url).href,
};

export function RegionFlag() {
  return <img className="w-5 h-5 shadow rounded-full" src={flags[region]} />;
}
