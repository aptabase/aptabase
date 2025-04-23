import { IconChevronDown } from "@tabler/icons-react";
import { useAtom } from "jotai";
import { atomWithStorage } from "jotai/utils";
import { useEffect, useState } from "react";
import { AppVersionDropdown } from "./filters/AppVersionDropdown";
import { CountryFilterDropdown } from "./filters/CountryFilterDropdown";
import { EventNameFilterDropdown } from "./filters/EventNameFilterDropdown";
import { OsFilterDropdown } from "./filters/OsFilterDropdown";

type TopFilterValue = {
  eventName?: string;
  osName?: string;
  country?: string;
  appVersion?: string;
};

type Props = {
  appId: string;
  onFiltersChange: (filters: TopFilterValue) => void;
};

const isFiltersExpandedAtom = atomWithStorage("user_sessions_filters_expanded", true);

export function UsersSessionsFilters({ appId, onFiltersChange }: Props) {
  const [isFiltersExpanded, setIsFiltersExpanded] = useAtom(isFiltersExpandedAtom);
  const [topFilters, setTopFilters] = useState<TopFilterValue>({});

  // notify parent of filter changes
  useEffect(() => {
    onFiltersChange(topFilters);
  }, [topFilters, onFiltersChange]);

  const updateFilters = (newFilters: Partial<TopFilterValue>) => {
    setTopFilters((prevFilters) => ({
      ...prevFilters,
      ...newFilters,
    }));
  };

  return (
    <div className="mb-4">
      <div className="flex flex-row items-center justify-between">
        <div
          className={`flex-1 grid transition-all duration-200 ${
            isFiltersExpanded ? "grid-rows-[1fr] opacity-100" : "grid-rows-[0fr] opacity-0"
          }`}
        >
          <div className="overflow-hidden">
            <div className="flex flex-row items-center gap-4 mt-4">
              <EventNameFilterDropdown appId={appId} onValueChange={(eventName) => updateFilters({ eventName })} />
              <OsFilterDropdown appId={appId} onValueChange={(osName) => updateFilters({ osName })} />
              <CountryFilterDropdown appId={appId} onValueChange={(country) => updateFilters({ country })} />
              <AppVersionDropdown appId={appId} onValueChange={(appVersion) => updateFilters({ appVersion })} />
            </div>
          </div>
        </div>

        <button
          onClick={() => setIsFiltersExpanded(!isFiltersExpanded)}
          className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground ml-4"
        >
          <IconChevronDown
            className={`h-3 w-3 transition-transform duration-200 ${isFiltersExpanded ? "rotate-0" : "-rotate-90"}`}
          />
          {isFiltersExpanded ? "Hide Filters" : "Show Filters"}
        </button>
      </div>
    </div>
  );
}
