import { topOperatingSystem } from "@features/analytics/query";
import { useAtom } from "jotai";
import { useEffect } from "react";
import { atomWithSearchParam } from "../../../../atoms/location-atoms";
import { FilterDropdownQuery } from "./FilterDropdownQuery";
import { FilterDropdownSelect } from "./FilterDropdownSelect";

type Props = {
  appId: string;
  onValueChange?: (value: string | undefined) => void;
};

const osAtom = atomWithSearchParam("os");

export function OsFilterDropdown(props: Props) {
  const [selectedOs, setSelectedOs] = useAtom(osAtom);

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
