import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { QueryParams, TopNItem } from "../query";
import { useApps } from "@features/apps";

type ChildrenProps = {
  isLoading: boolean;
  isError: boolean;
  items: TopNItem[];
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
  const period = searchParams.get("period") || "";
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const { isLoading, isError, data } = useQuery(
    [props.queryName, buildMode, props.appId, period, countryCode, appVersion, eventName, osName],
    () =>
      props.query({
        buildMode,
        appId: props.appId,
        period,
        countryCode,
        appVersion,
        eventName,
        osName,
      })
  );

  return props.children({ isLoading, isError, items: data || [] });
}
