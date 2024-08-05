import { LazyLoad } from "@components/LazyLoad";
import { Page, PageHeading } from "@components/Page";
import { useApps, useCurrentApp } from "@features/apps";
import { Navigate, useNavigate } from "react-router-dom";
import { CurrentFilters } from "./CurrentFilters";
import { CountryWidget } from "./dashboard/CountryWidget";
import { EventWidget } from "./dashboard/EventWidget";
import { OSWidget } from "./dashboard/OSWidget";
import { OnboardingDashboard } from "./dashboard/OnboardingDashboard";
import { TeaserDashboardContainer } from "./dashboard/TeaserDashboardContainer";
import { VersionWidget } from "./dashboard/VersionWidget";
import { DateFilterContainer } from "./date-filters/DateFilterContainer";
import { MainChartWidget } from "./key_metrics/MainChartWidget";
import { AppLockedContent } from "./locked/AppLockedContent";
import { BuildModeSelector } from "./mode/BuildModeSelector";
import { DebugModeBanner } from "./mode/DebugModeBanner";

Component.displayName = "DashboardPage";

export function Component() {
  const { buildMode } = useApps();
  const app = useCurrentApp();
  const navigate = useNavigate();

  if (!app) return <Navigate to="/" />;
  if (app.lockReason) {
    return (
      <TeaserDashboardContainer app={app}>
        <AppLockedContent reason={app.lockReason} />
      </TeaserDashboardContainer>
    );
  }

  if (!app.hasEvents) return <OnboardingDashboard app={app} />;

  const resetFilters = () => navigate(`/${app.id}/`);
  const containerClassName = "min-h-[12rem] bg-background py-4 sm:px-4";

  return (
    <Page title={app.name}>
      {buildMode === "debug" && <DebugModeBanner />}
      <div className="space-y-4">
        <div className="flex justify-between items-center">
          <PageHeading title="Dashboard" onClick={resetFilters} />
          <div className="flex items-end space-x-2">
            <BuildModeSelector />
            <DateFilterContainer />
          </div>
        </div>
        <div className="flex w-full justify-end">
          <CurrentFilters />
        </div>
        <MainChartWidget appId={app.id} appName={app.name} />
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
