import { topCountries } from "@features/analytics/query";
import { useEffect, useState } from "react";
import { FilterDropdownQuery } from "./FilterDropdownQuery";
import { FilterDropdownSelect } from "./FilterDropdownSelect";

type Props = {
  appId: string;
  onValueChange?: (value: string | undefined) => void;
};

export function CountryFilterDropdown(props: Props) {
  const [selectedCountry, setSelectedCountry] = useState<string | undefined>();

  if (props.onValueChange) {
    useEffect(() => {
      props.onValueChange?.(selectedCountry);
    }, [selectedCountry]);
  }

  return (
    <FilterDropdownQuery appId={props.appId} queryKey={"sessions-country-dropdown"} query={topCountries}>
      {(data) => (
        <FilterDropdownSelect
          value={selectedCountry}
          onValueChange={setSelectedCountry}
          data={data}
          placeholder="Select Country"
        />
      )}
    </FilterDropdownQuery>
  );
}
