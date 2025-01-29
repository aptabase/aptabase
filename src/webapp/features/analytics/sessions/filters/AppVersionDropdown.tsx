import { topAppVersions } from "@features/analytics/query";
import { useAtom } from "jotai";
import { useEffect } from "react";
import { atomWithSearchParam } from "../../../../atoms/location-atoms";
import { FilterDropdownQuery } from "./FilterDropdownQuery";
import { FilterDropdownSelect } from "./FilterDropdownSelect";

type Props = {
  appId: string;
  onValueChange?: (value: string | undefined) => void;
};

const appVersionAtom = atomWithSearchParam("appVersion");

export function AppVersionDropdown(props: Props) {
  const [selectedAppVersion, setSelectedAppVersion] = useAtom(appVersionAtom);

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
