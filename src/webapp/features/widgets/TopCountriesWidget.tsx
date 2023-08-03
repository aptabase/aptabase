import { TopNChart } from "./charts";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { getCountryFlagUrl, getCountryName } from "./countries";
import { topCountries } from "./query";
import { useApps } from "../apps";

type Props = {
  appId: string;
};

export function TopCountriesWidget(props: Props) {
  const { buildMode } = useApps();
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
    ["top-countries", buildMode, props.appId, period, countryCode, appVersion, eventName, osName],
    () =>
      topCountries({
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
      title="Countries"
      searchParamKey="countryCode"
      isLoading={isLoading}
      isError={isError}
      labels={["Name", "Sessions"]}
      items={rows || []}
      renderRow={(item) => (
        <span className="flex items-center space-x-2 px-2">
          <img src={getCountryFlagUrl(item.name)} className="h-5 w-5 shadow rounded-full" />
          <p>{getCountryName(item.name) || "Unknown"}</p>
        </span>
      )}
    />
  );
}
