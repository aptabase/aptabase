import { QueryObserverResult, RefetchOptions, useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { QueryParams, TopNItem } from "../query";
import { useApps } from "@features/apps";
import { useDatePicker } from "@hooks/use-datepicker";

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
  const { startDate, endDate, granularity } = useDatePicker();
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const { isLoading, isError, data, refetch } = useQuery({
    queryKey: [props.queryName, buildMode, props.appId, startDate, endDate, countryCode, appVersion, eventName, osName],
    queryFn: () =>
      props.query({
        buildMode,
        appId: props.appId,
        startDate,
        endDate,
        granularity,
        countryCode,
        appVersion,
        eventName,
        osName,
      }),
    staleTime: 10000,
  });

  return props.children({ isLoading, isError, items: data || [], refetch });
}
