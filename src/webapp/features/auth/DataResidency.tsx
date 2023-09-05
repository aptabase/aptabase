import { region } from "@features/env";
import { RegionFlag } from "@components/RegionFlag";

export function DataResidency() {
  if (!region) return null;

  return (
    <div className="justify-center text-xs tracking-normal text-muted-foreground flex items-center p-2">
      <div className="p-1">
        <RegionFlag />
      </div>
      <p>{region.toUpperCase()} Data Residency</p>
    </div>
  );
}
