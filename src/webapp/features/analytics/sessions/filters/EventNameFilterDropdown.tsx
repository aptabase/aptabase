import { topEvents } from "@features/analytics/query";
import { useEffect, useState } from "react";
import { FilterDropdownQuery } from "./FilterDropdownQuery";
import { FilterDropdownSelect } from "./FilterDropdownSelect";

type Props = {
  appId: string;
  onValueChange?: (value: string | undefined) => void;
};

export function EventNameFilterDropdown(props: Props) {
  const [selectedEvent, setSelectedEvent] = useState<string | undefined>();

  if (props.onValueChange) {
    useEffect(() => {
      props.onValueChange?.(selectedEvent);
    }, [selectedEvent]);
  }

  return (
    <FilterDropdownQuery appId={props.appId} queryKey={"top-events"} query={topEvents}>
      {(data) => (
        <FilterDropdownSelect
          value={selectedEvent}
          onValueChange={setSelectedEvent}
          data={data}
          placeholder="Select Event"
        />
      )}
    </FilterDropdownQuery>
  );
}
