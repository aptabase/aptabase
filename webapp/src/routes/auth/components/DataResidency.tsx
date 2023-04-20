import { RegionFlag, region } from "@app/primitives";

export function DataResidency() {
  return (
    <div className="justify-center text-xs tracking-normal text-secondary flex items-center p-2">
      <div className="p-1">
        <RegionFlag />
      </div>
      <p>{region.toUpperCase()} Data Residency</p>
    </div>
  );
}
