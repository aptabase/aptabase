import { useApps } from "@features/apps";
import { QueryObserverResult, RefetchOptions, useQuery } from "@tanstack/react-query";
import { useAtomValue } from "jotai";
import { useSearchParams } from "react-router-dom";
import { dateFilterValuesAtom } from "../../../atoms/date-atoms";
import { QueryParams, TopNItem } from "../query";

type ChildrenProps = {
  isLoading: boolean;
  isError: boolean;
  items: TopNItem[];
  refetch?: (options?: RefetchOptions | undefined) => Promise<QueryObserverResult<TopNItem[], Error>>;
};

type Props = {
  appId: string;
  queryName: string;
  query: (params: QueryParams) => Promise<TopNItem[]>;
  children: (props: ChildrenProps) => JSX.Element;
};

export function TopNDataContainer(props: Props) {
  const { buildMode } = useApps();
  const [searchParams] = useSearchParams();
  const { startDateIso, endDateIso, granularity } = useAtomValue(dateFilterValuesAtom);

  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const { isLoading, isError, data, refetch } = useQuery({
    queryKey: [
      props.queryName,
      buildMode,
      props.appId,
      startDateIso,
      endDateIso,
      countryCode,
      appVersion,
      eventName,
      osName,
    ],
    queryFn: () =>
      props.query({
        buildMode,
        appId: props.appId,
        startDate: startDateIso,
        endDate: endDateIso,
        granularity,
        countryCode,
        appVersion,
        eventName,
        osName,
      }),
    staleTime: 10000,
    enabled: !!startDateIso && !!endDateIso && !!granularity,
  });

  return props.children({ isLoading, isError, items: data || [], refetch });
}
