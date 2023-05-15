import { useCurrentApp } from "@app/navigation";
import { DateRangePicker, Head, PageHeading } from "@app/primitives";
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

Component.displayName = "Dashboard";
export function Component() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const eventName = searchParams.get("eventName") || "";
  const countryCode = searchParams.get("countryCode") || "";
  const osName = searchParams.get("osName") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const app = useCurrentApp();

  const resetFilters = () => navigate("/");

  return (
    <>
      <Head title={app.name} />
      <div className="space-y-4">
        <div className="flex justify-between items-end">
          <PageHeading title="Dashboard" onClick={resetFilters} />
          <DateRangePicker />
        </div>
        <MainChartWidget appId={app.id} />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {countryCode ? (
            <TopRegionsWidget appId={app.id} />
          ) : (
            <TopCountriesWidget appId={app.id} />
          )}
          {osName ? (
            <TopOSVersionsWidget appId={app.id} />
          ) : (
            <TopOperatingSystemsWidget appId={app.id} />
          )}
          {eventName ? (
            <EventPropsWidget appId={app.id} />
          ) : (
            <TopEventsWidget appId={app.id} />
          )}
          {appVersion ? (
            <TopAppBuildNumbersWidget appId={app.id} />
          ) : (
            <TopAppVersionsWidget appId={app.id} />
          )}
        </div>
      </div>
    </>
  );
}
