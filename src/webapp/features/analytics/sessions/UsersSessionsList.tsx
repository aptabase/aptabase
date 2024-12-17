import { Button } from "@components/Button";
import { SessionStartEndDateFilters, SessionsGridDisplay } from "@features/analytics/sessions/SessionsGridDisplay";
import { IconChevronLeft, IconChevronRight, IconChevronsLeft } from "@tabler/icons-react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { useLocation } from "react-router-dom";
import { QueryParams, historicalSessions } from "../query";
import { UsersSessionsFilters } from "./UsersSessionsFilters";

const SESSIONS_PAGE_SIZE = 10;

type Props = {
  appId: string;
  buildMode: "release" | "debug";
};

type TopFilterValue = {
  eventName?: string;
  osName?: string;
  country?: string;
  appVersion?: string;
};

export function UserSessionsList(props: Props) {
  const location = useLocation();
  const locationStateFilters = location.state?.sessionFilters;
  const [dateFilters, setDateFilters] = useState<SessionStartEndDateFilters>(locationStateFilters || {});
  const [topFilters, setTopFilters] = useState<TopFilterValue>({});

  const requestParams: QueryParams = useMemo(() => {
    const countryCode = !topFilters.country ? "" : topFilters.country;
    const appVersion = !topFilters.appVersion ? "" : topFilters.appVersion;
    const allParams: QueryParams = {
      appId: props.appId,
      buildMode: props.buildMode,
      startDate: dateFilters.startDate,
      endDate: dateFilters.endDate,
      sessionId: dateFilters.sessionId,
      eventName: topFilters.eventName,
      osName: topFilters.osName,
      countryCode,
      appVersion,
    };

    const params = Object.fromEntries(Object.entries(allParams).filter(([_, v]) => v != null));
    return params as QueryParams;
  }, [props, dateFilters, topFilters]);

  const { data, isSuccess, isPlaceholderData } = useQuery({
    queryKey: ["historical-sessions", props.appId, props.buildMode, dateFilters, topFilters],
    queryFn: () => historicalSessions(requestParams),
    placeholderData: keepPreviousData,
  });

  const sessions = [...(data || [])];
  if (dateFilters.startDate) {
    sessions.reverse();
  }

  // When we reach first page and hit previous one more time
  useEffect(() => {
    if (isSuccess && sessions.length === 0 && dateFilters.startDate) {
      setDateFilters({});
    }
  }, [sessions]);

  // Data for pager
  const firstSession = sessions[0];
  const lastSession = sessions[sessions.length - 1];
  const previousPageClick = () => {
    setDateFilters({
      sessionId: firstSession?.id,
      startDate: firstSession?.startedAt,
    });
  };
  const nextPageClick = () => {
    setDateFilters({
      endDate: lastSession?.startedAt,
      sessionId: lastSession?.id,
    });
  };
  const firstPageClick = () => {
    setDateFilters({});
  };

  return (
    <div className="flex flex-col min-h-[calc(100vh-120px)]">
      <UsersSessionsFilters appId={props.appId} onFiltersChange={setTopFilters} />
      <div className="mb-auto">
        <SessionsGridDisplay
          appId={props.appId}
          sessions={sessions}
          sessionFilters={dateFilters}
          isLoading={isPlaceholderData}
        />
      </div>

      <div className="flex justify-end mt-4 mr-16 ">
        {!!dateFilters.sessionId && (
          <Button disabled={isPlaceholderData} variant="ghost" onClick={firstPageClick}>
            <IconChevronsLeft></IconChevronsLeft>
            First
          </Button>
        )}
        {(!!dateFilters.sessionId || isPlaceholderData) && (
          <Button disabled={isPlaceholderData} variant="ghost" onClick={previousPageClick}>
            <IconChevronLeft></IconChevronLeft>
            Previous
          </Button>
        )}
        {(sessions.length >= SESSIONS_PAGE_SIZE || isPlaceholderData) && (
          <Button disabled={isPlaceholderData} variant="ghost" onClick={nextPageClick}>
            Next
            <IconChevronRight></IconChevronRight>
          </Button>
        )}
      </div>
    </div>
  );
}
