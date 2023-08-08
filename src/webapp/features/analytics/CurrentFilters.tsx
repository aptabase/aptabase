import { Link, useSearchParams } from "react-router-dom";
import { IconX } from "@tabler/icons-react";
import { Country } from "./dashboard/Country";
import { OS } from "./dashboard/OS";

type ChipProps = {
  children: React.ReactNode;
  removeProperty: string;
};

function FilterChip(props: ChipProps) {
  const targetUrl = new URL(window.location.href);
  targetUrl.searchParams.delete(props.removeProperty);

  return (
    <span className="inline-flex items-center relative group border rounded py-2 px-2 text-sm min-w-[6rem] bg-muted">
      <div className="flex items-center space-x-1 group-hover:blur-sm group-hover:opacity-50 transition-all">
        {props.children}
      </div>
      <Link
        to={targetUrl.toString()}
        className="hidden absolute w-full h-full group-hover:flex"
        preventScrollReset={true}
      >
        <span className="flex space-x-2 items-center text-destructive">
          <IconX strokeWidth={1.5} className="h-5 w-5" />
          <span>Remove</span>
        </span>
      </Link>
    </span>
  );
}

export function CurrentFilters() {
  const [searchParams] = useSearchParams();
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  return (
    <div className="flex items-center gap-2 flex-wrap">
      {eventName && (
        <FilterChip removeProperty="eventName">
          <span>Event =</span>
          <span className="font-bold">{eventName}</span>
        </FilterChip>
      )}
      {appVersion && (
        <FilterChip removeProperty="appVersion">
          <span>Version =</span>
          <span className="font-bold">{appVersion}</span>
        </FilterChip>
      )}
      {osName && (
        <FilterChip removeProperty="osName">
          <OS name={osName} />
        </FilterChip>
      )}
      {countryCode && (
        <FilterChip removeProperty="countryCode">
          <Country countryCode={countryCode} />
        </FilterChip>
      )}
    </div>
  );
}
