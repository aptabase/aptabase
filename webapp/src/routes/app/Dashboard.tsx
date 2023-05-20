import { useCurrentApp } from "@app/navigation";
import { Card, DateRangePicker, Head, PageHeading } from "@app/primitives";
import {
  EventPropsWidget,
  MainChartWidget,
  TopAppBuildNumbersWidget,
  TopAppVersionsWidget,
  TopCountriesWidget,
  TopEventsWidget,
  TopOSVersionsWidget,
  TopOperatingSystemsWidget,
  TopRegionsWidget,
} from "@app/widgets";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useApps } from "@app/apps";

Component.displayName = "Dashboard";

export function Component() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const eventName = searchParams.get("eventName") || "";
  const countryCode = searchParams.get("countryCode") || "";
  const osName = searchParams.get("osName") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const { buildMode } = useApps();
  const app = useCurrentApp();

  const resetFilters = () => navigate("/");

  return (
    <>
      <Head title={app.name} />
      <div className="space-y-4">
        <div className="flex justify-between items-end">
          <div className="flex space-x-2 items-center">
            <PageHeading title="Dashboard" onClick={resetFilters} />
            {buildMode === "debug" && (
              <span className="inline-flex rounded bg-orange-300/10 px-2 py-1 font-medium text-xs sm:text-sm text-orange-700">
                Debug
              </span>
            )}
          </div>
          <DateRangePicker />
        </div>
        <MainChartWidget appId={app.id} />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-[1px] pt-[1px] bg-gray-200">
          {countryCode ? (
            <Card>
              <TopRegionsWidget appId={app.id} />
            </Card>
          ) : (
            <Card>
              <TopCountriesWidget appId={app.id} />
            </Card>
          )}
          {osName ? (
            <Card>
              <TopOSVersionsWidget appId={app.id} />
            </Card>
          ) : (
            <Card>
              <TopOperatingSystemsWidget appId={app.id} />
            </Card>
          )}
          {eventName ? (
            <Card>
              <EventPropsWidget appId={app.id} />
            </Card>
          ) : (
            <Card>
              <TopEventsWidget appId={app.id} />
            </Card>
          )}
          {appVersion ? (
            <Card>
              <TopAppBuildNumbersWidget appId={app.id} />
            </Card>
          ) : (
            <Card>
              <TopAppVersionsWidget appId={app.id} />
            </Card>
          )}
        </div>
      </div>
    </>
  );
}
