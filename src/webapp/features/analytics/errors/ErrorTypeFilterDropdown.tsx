import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@components/Select";
import { useApps } from "@features/apps";
import { useQuery } from "@tanstack/react-query";
import { useAtomValue } from "jotai";
import { dateFilterValuesAtom } from "../../../atoms/date-atoms";

// Matches the top-N shape ({ name, value }) returned by the backend,
// where value is the number of occurrences within the date range.
type ErrorTypeItem = {
  name: string;
  value: number;
};

async function fetchErrorTypes(
  appId: string,
  buildMode: string,
  startDate?: string,
  endDate?: string,
): Promise<ErrorTypeItem[]> {
  const params = new URLSearchParams({ buildMode });
  if (startDate) params.set("startDate", startDate);
  if (endDate) params.set("endDate", endDate);

  const response = await fetch(`/api/v0/apps/${appId}/errors/types?${params}`, {
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error("Failed to fetch error types");
  }

  return response.json();
}

type Props = {
  appId: string;
  value: string;
  onValueChange: (value: string) => void;
};

export function ErrorTypeFilterDropdown(props: Props) {
  const { buildMode } = useApps();
  const dateFilters = useAtomValue(dateFilterValuesAtom);

  const { data } = useQuery({
    queryKey: ["errors-types-dropdown", props.appId, buildMode, dateFilters.startDateIso, dateFilters.endDateIso],
    queryFn: () => fetchErrorTypes(props.appId, buildMode, dateFilters.startDateIso, dateFilters.endDateIso),
    staleTime: 10000,
  });

  // Items arrive ordered by occurrence count (most frequent first)
  const items = (data ?? []).filter((item) => !!item.name);

  // Keep the active filter selectable/visible even when it no longer appears
  // in the current date range (e.g. after narrowing the range)
  const showSelectedFallback = props.value !== "all" && !items.some((item) => item.name === props.value);

  return (
    <Select value={props.value} onValueChange={props.onValueChange}>
      <SelectTrigger className="w-48">
        <SelectValue />
      </SelectTrigger>
      <SelectContent className="max-h-[410px] overflow-y-auto">
        <SelectItem value="all">All Types</SelectItem>
        {showSelectedFallback && <SelectItem value={props.value}>{props.value}</SelectItem>}
        {items.map((item) => (
          <SelectItem key={item.name} value={item.name}>
            {item.name} ({item.value})
          </SelectItem>
        ))}
        {items.length === 0 && !showSelectedFallback && (
          <div className="text-muted-foreground py-1.5 pl-8 pr-2 text-sm">No error types in range</div>
        )}
      </SelectContent>
    </Select>
  );
}
