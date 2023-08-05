import { TopNChart } from "./charts";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { CardTitle } from "./CardTitle";
import { getCountryFlagUrl, getCountryName } from "./countries";
import { topRegions } from "./query";
import { useApps } from "../apps";

type Props = {
  appId: string;
};

export function TopRegionsWidget(props: Props) {
  const { buildMode } = useApps();
  const [searchParams] = useSearchParams();
  const period = searchParams.get("period") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";
  const countryCode = searchParams.get("countryCode") || "";

  const {
    isLoading,
    isError,
    data: rows,
  } = useQuery(
    ["top-regions", buildMode, props.appId, period, countryCode, appVersion, eventName, osName],
    () =>
      topRegions({
        buildMode,
        appId: props.appId,
        period,
        countryCode,
        appVersion,
        eventName,
        osName,
      })
  );

  const targetUrl = new URL(window.location.href);
  targetUrl.searchParams.delete("countryCode");

  return (
    <TopNChart
      title={
        <CardTitle backProperty="countryCode">
          <div className="flex items-center space-x-2">
            <img src={getCountryFlagUrl(countryCode)} className="h-5 w-5 shadow rounded-full" />
            <span>{getCountryName(countryCode) || "Unknown"}</span>
          </div>
        </CardTitle>
      }
      isLoading={isLoading}
      isError={isError}
      valueLabel="Sessions"
      items={rows || []}
    />
  );
}
