import { topEvents } from "@features/analytics/query";
import { useAtom } from "jotai";
import { useEffect } from "react";
import { atomWithSearchParam } from "../../../../atoms/location-atoms";
import { FilterDropdownFancy } from "./FilterDropdownFancy";
import { FilterDropdownQuery } from "./FilterDropdownQuery";

type Props = {
  appId: string;
  onValueChange?: (value: string | undefined) => void;
};

const eventNameAtom = atomWithSearchParam("eventName");

export function EventNameFilterDropdown(props: Props) {
  const [selectedEvent, setSelectedEvent] = useAtom(eventNameAtom);

  if (props.onValueChange) {
    useEffect(() => {
      props.onValueChange?.(selectedEvent);
    }, [selectedEvent]);
  }

  return (
    <FilterDropdownQuery appId={props.appId} queryKey={"top-events"} query={topEvents}>
      {(data) => (
        <FilterDropdownFancy
          value={selectedEvent}
          onValueChange={setSelectedEvent}
          data={data}
          placeholder="Select Event"
        />
      )}
    </FilterDropdownQuery>
  );
}
