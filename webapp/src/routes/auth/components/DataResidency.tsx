import { isManagedCloud, region } from "@app/env";
import { RegionFlag } from "@app/primitives";

export function DataResidency() {
  if (!isManagedCloud) return null;

  return (
    <div className="justify-center text-xs tracking-normal text-muted-foreground flex items-center p-2">
      <div className="p-1">
        <RegionFlag />
      </div>
      <p>{region.toUpperCase()} Data Residency</p>
    </div>
  );
}
