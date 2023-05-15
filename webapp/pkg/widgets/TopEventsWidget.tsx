import { TopNChart } from "@app/charts";
import { Card } from "@app/primitives";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { topEvents } from "./query";

type Props = {
  appId: string;
};

export function TopEventsWidget(props: Props) {
  const [searchParams] = useSearchParams();
  const period = searchParams.get("period") || "";
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const {
    isLoading,
    isError,
    data: rows,
  } = useQuery(
    [
      "top-events",
      props.appId,
      period,
      countryCode,
      appVersion,
      eventName,
      osName,
    ],
    () =>
      topEvents({
        appId: props.appId,
        period,
        countryCode,
        appVersion,
        eventName,
        osName,
      })
  );

  return (
    <TopNChart
      title="Events"
      searchParamKey="eventName"
      isLoading={isLoading}
      isError={isError}
      labels={["Name", "Count"]}
      items={rows || []}
    />
  );
}
