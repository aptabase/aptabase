import { TopNChart } from "./charts";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { CardTitle } from "./CardTitle";
import { topOSVersions } from "./query";
import { useApps } from "../apps";
import { OSIcon } from "./icons/os";

type Props = {
  appId: string;
};

export function TopOSVersionsWidget(props: Props) {
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
    ["top-osversions", buildMode, props.appId, period, countryCode, appVersion, eventName, osName],
    () =>
      topOSVersions({
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
      title={
        <CardTitle backProperty="osName">
          <span className="flex items-center space-x-2 px-2">
            <OSIcon name={osName} className="h-5 w-5" />
            <p>{osName || "Unknown"}</p>
          </span>
        </CardTitle>
      }
      isLoading={isLoading}
      isError={isError}
      labels={["Version", "Daily Users"]}
      items={rows || []}
    />
  );
}
