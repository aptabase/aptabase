import { DateRangePicker, LazyLoad, Page, PageHeading } from "@features/primitives";
import { Navigate, useNavigate, useSearchParams } from "react-router-dom";
import { useApps, useCurrentApp } from "@features/apps";
import { DebugModeBanner } from "./mode/DebugModeBanner";
import { AppConfigMenu } from "./mode/AppConfigMenu";
import { CurrentFilters } from "./CurrentFilters";
import { MainChartWidget } from "./key_metrics/MainChartWidget";
import { TopEventPropsWidget } from "./top_n/TopEventPropsWidget";
import { TopAppBuildNumbersWidget } from "./top_n/TopAppBuildNumbersWidget";
import { TopAppVersionsWidget } from "./top_n/TopAppVersionsWidget";
import { TopCountriesWidget } from "./top_n/TopCountriesWidget";
import { TopEventsWidget } from "./top_n/TopEventsWidget";
import { TopOSVersionsWidget } from "./top_n/TopOSVersionsWidget";
import { TopOperatingSystemsWidget } from "./top_n/TopOperatingSystemsWidget";
import { TopRegionsWidget } from "./top_n/TopRegionsWidget";

Component.displayName = "DashboardPage";

export function Component() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const eventName = searchParams.get("eventName") || "";
  const countryCode = searchParams.get("countryCode") || "";
  const osName = searchParams.get("osName") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const { buildMode } = useApps();
  const app = useCurrentApp();

  if (!app) return <Navigate to="/" />;

  const resetFilters = () => navigate(`/${app.id}/`);

  const containerClassName = "min-h-[12rem] bg-background py-4 sm:px-4";

  return (
    <Page title={app.name}>
      {buildMode === "debug" && <DebugModeBanner />}
      <div className="space-y-4">
        <div className="flex justify-between items-center">
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
        <div className="grid grid-cols-1 md:grid-cols-2 gap-[1px] pt-[1px] bg-accent">
          <LazyLoad className={containerClassName}>
            {countryCode ? (
              <TopRegionsWidget appId={app.id} />
            ) : (
              <TopCountriesWidget appId={app.id} />
            )}
          </LazyLoad>
          <LazyLoad className={containerClassName}>
            {osName ? (
              <TopOSVersionsWidget appId={app.id} />
            ) : (
              <TopOperatingSystemsWidget appId={app.id} />
            )}
          </LazyLoad>
          <LazyLoad className={containerClassName}>
            {eventName ? (
              <TopEventPropsWidget appId={app.id} />
            ) : (
              <TopEventsWidget appId={app.id} />
            )}
          </LazyLoad>
          <LazyLoad className={containerClassName}>
            {appVersion ? (
              <TopAppBuildNumbersWidget appId={app.id} />
            ) : (
              <TopAppVersionsWidget appId={app.id} />
            )}
          </LazyLoad>
        </div>
      </div>
    </Page>
  );
}
