import { topCountries } from "@features/analytics/query";
import { useAtom } from "jotai";
import { useEffect } from "react";
import { atomWithSearchParam } from "../../../../atoms/location-atoms";
import { FilterDropdownQuery } from "./FilterDropdownQuery";
import { FilterDropdownSelect } from "./FilterDropdownSelect";

type Props = {
  appId: string;
  onValueChange?: (value: string | undefined) => void;
};

const countryAtom = atomWithSearchParam("country");

export function CountryFilterDropdown(props: Props) {
  const [selectedCountry, setSelectedCountry] = useAtom(countryAtom);

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
