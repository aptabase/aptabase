import { topAppVersions } from "@features/analytics/query";
import { useEffect, useState } from "react";
import { FilterDropdownQuery } from "./FilterDropdownQuery";
import { FilterDropdownSelect } from "./FilterDropdownSelect";

type Props = {
  appId: string;
  onValueChange?: (value: string | undefined) => void;
};

export function AppVersionDropdown(props: Props) {
  const [selectedAppVersion, setSelectedAppVersion] = useState<string | undefined>();

  if (props.onValueChange) {
    useEffect(() => {
      props.onValueChange?.(selectedAppVersion);
    }, [selectedAppVersion]);
  }

  return (
    <FilterDropdownQuery appId={props.appId} queryKey={"sessions-app-version-dropdown"} query={topAppVersions}>
      {(data) => (
        <FilterDropdownSelect
          value={selectedAppVersion}
          onValueChange={setSelectedAppVersion}
          data={data}
          placeholder="Select App Version"
        />
      )}
    </FilterDropdownQuery>
  );
}
