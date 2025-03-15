import { topEvents } from "@features/analytics/query";
import { atom, useAtom } from "jotai";
import { useEffect, useMemo } from "react";
import { atomWithSearchParam } from "../../../../atoms/location-atoms";
import { FilterDropdownFancy } from "./FilterDropdownFancy";
import { FilterDropdownQuery } from "./FilterDropdownQuery";

type Props = {
  appId: string;
  onValueChange?: (value: string | undefined) => void;
  onRemove?: () => void;
  localValue?: string;
  localPersistence?: boolean;
  className?: string;
};

const eventNameAtom = atomWithSearchParam("eventName");

export function EventNameFilterDropdown(props: Props) {
  const localAtom = useMemo(
    () => (props.localPersistence ? atom<string | undefined>(props.localValue) : null),
    [props.localValue, props.localPersistence]
  );

  const [selectedEvent, setSelectedEvent] = useAtom(localAtom ?? eventNameAtom);

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
          onRemove={props.onRemove}
          data={data}
          placeholder="Select Event"
          className={props.className}
        />
      )}
    </FilterDropdownQuery>
  );
}
