import { TopNChart } from "./charts";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { CardTitle } from "./CardTitle";
import { topAppBuildNumbers } from "./query";
import { useApps } from "../apps";

type Props = {
  appId: string;
};

export function TopAppBuildNumbersWidget(props: Props) {
  const { buildMode } = useApps();
  const [searchParams] = useSearchParams();
  const period = searchParams.get("period") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const countryCode = searchParams.get("countryCode") || "";
  const osName = searchParams.get("osName") || "";

  const {
    isLoading,
    isError,
    data: rows,
  } = useQuery(
    [
      "top-appbuildnumbers",
      buildMode,
      props.appId,
      period,
      countryCode,
      appVersion,
      eventName,
      osName,
    ],
    () =>
      topAppBuildNumbers({
        buildMode,
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
      title={<CardTitle backProperty="appVersion">{appVersion}</CardTitle>}
      isLoading={isLoading}
      isError={isError}
      valueLabel="Sessions"
      items={rows || []}
    />
  );
}
