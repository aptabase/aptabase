import { useCurrentApp } from "@app/navigation";
import { Card, DateRangePicker, Head, PageHeading } from "@app/primitives";
import {
  CurrentFilters,
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
import { AppConfigMenu, useApps } from "@app/apps";
import { DebugModeBanner } from "./components/DebugModeBanner";

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
      {buildMode === "debug" && <DebugModeBanner />}
      <div className="space-y-4">
        <div className="flex justify-between items-end">
          <PageHeading title="Dashboard" onClick={resetFilters} />
          <div className="flex items-center">
            <AppConfigMenu />
            <DateRangePicker />
          </div>
        </div>
        <div className="flex w-full justify-end">
          <CurrentFilters />
        </div>
        <MainChartWidget appId={app.id} />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-[1px] pt-[1px] bg-subtle">
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
