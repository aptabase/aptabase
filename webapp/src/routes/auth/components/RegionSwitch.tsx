import { isManagedCloud, region } from "@app/env";

export function RegionSwitch() {
  if (!isManagedCloud) return null;

  const otherRegion = region === "eu" ? "us" : "eu";
  const href = `https://${otherRegion}.aptabase.com`;

  return (
    <div className="justify-center text-xs tracking-normal text-muted-foreground flex items-center p-2">
      Looking for {otherRegion.toUpperCase()} hosting?
      <a href={href} className="text-primary ml-1">
        Switch →
      </a>
    </div>
  );
}
