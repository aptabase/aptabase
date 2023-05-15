import { TopNChart } from "@app/charts";
import { Card } from "@app/primitives";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { getOperatingSystemImageUrl } from "./os";
import { topOperatingSystem } from "./query";

type Props = {
  appId: string;
};

export function TopOperatingSystemsWidget(props: Props) {
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
      "top-operatingsystems",
      props.appId,
      period,
      countryCode,
      appVersion,
      eventName,
      osName,
    ],
    () =>
      topOperatingSystem({
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
      title="Operating Systems"
      searchParamKey="osName"
      isLoading={isLoading}
      isError={isError}
      labels={["Name", "Sessions"]}
      items={rows || []}
      renderRow={(item) => (
        <span className="flex items-center space-x-2 px-2">
          <img
            src={getOperatingSystemImageUrl(item.name)}
            className="h-5 w-5"
          />
          <p>{item.name || "Unknown"}</p>
        </span>
      )}
    />
  );
}
