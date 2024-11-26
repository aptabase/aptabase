import { topOperatingSystem } from "@features/analytics/query";
import { useEffect, useState } from "react";
import { FilterDropdownQuery } from "./FilterDropdownQuery";
import { FilterDropdownSelect } from "./FilterDropdownSelect";

type Props = {
  appId: string;
  onValueChange?: (value: string | undefined) => void;
};

export function OsFilterDropdown(props: Props) {
  const [selectedOs, setSelectedOs] = useState<string | undefined>();

  if (props.onValueChange) {
    useEffect(() => {
      props.onValueChange?.(selectedOs);
    }, [selectedOs]);
  }

  return (
    <FilterDropdownQuery appId={props.appId} queryKey={"sessions-os-dropdown"} query={topOperatingSystem}>
      {(data) => (
        <FilterDropdownSelect value={selectedOs} onValueChange={setSelectedOs} data={data} placeholder="Select OS" />
      )}
    </FilterDropdownQuery>
  );
}
