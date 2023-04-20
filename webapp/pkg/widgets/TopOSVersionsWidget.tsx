import { TopNChart } from "@app/charts";
import { Card } from "@app/primitives";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { CardTitle } from "./CardTitle";
import { getOperatingSystemImageUrl } from "./os";
import { topOSVersions } from "./query";

type Props = {
  appId: string;
};

export function TopOSVersionsWidget(props: Props) {
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
  } = useQuery(["top-osversions", props.appId, period, countryCode, appVersion, eventName, osName], () =>
    topOSVersions({ appId: props.appId, period, countryCode, appVersion, eventName, osName })
  );

  return (
    <Card>
      <TopNChart
        title={
          <CardTitle backProperty="osName">
            <span className="flex items-center space-x-2 px-2">
              <img src={getOperatingSystemImageUrl(osName)} className="h-5 w-5" />
              <p>{osName || "Unknown"}</p>
            </span>
          </CardTitle>
        }
        isLoading={isLoading}
        isError={isError}
        labels={["Version", "Sessions"]}
        items={rows || []}
      />
    </Card>
  );
}
