import { DateRangePicker, LazyLoad, Page, PageHeading } from "@features/primitives";
import { Navigate, useNavigate } from "react-router-dom";
import { useApps, useCurrentApp } from "@features/apps";
import { DebugModeBanner } from "./mode/DebugModeBanner";
import { AppConfigMenu } from "./mode/AppConfigMenu";
import { CurrentFilters } from "./CurrentFilters";
import { MainChartWidget } from "./key_metrics/MainChartWidget";
import { OSWidget } from "./dashboard/OSWidget";
import { CountryWidget } from "./dashboard/CountryWidget";
import { VersionWidget } from "./dashboard/VersionWidget";
import { EventWidget } from "./dashboard/EventWidget";
import { OnboardingDashboard } from "@features/onboarding";

Component.displayName = "DashboardPage";

export function Component() {
  const { buildMode } = useApps();
  const app = useCurrentApp();
  const navigate = useNavigate();

  if (!app) return <Navigate to="/" />;
  if (!app.hasEvents) return <OnboardingDashboard app={app} />;

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
            <CountryWidget appId={app.id} />
          </LazyLoad>
          <LazyLoad className={containerClassName}>
            <OSWidget appId={app.id} />
          </LazyLoad>
          <LazyLoad className={containerClassName}>
            <EventWidget appId={app.id} />
          </LazyLoad>
          <LazyLoad className={containerClassName}>
            <VersionWidget appId={app.id} />
          </LazyLoad>
        </div>
      </div>
    </Page>
  );
}
