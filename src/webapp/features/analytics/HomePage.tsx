import { Page, PageHeading } from "@components/Page";
import { useApps } from "@features/apps";
import { LonelyState } from "./LonelyState";
import { trackEvent } from "@aptabase/web";
import { useEffect } from "react";
import { BuildModeSelector } from "./mode/BuildModeSelector";
import { DebugModeBanner } from "./mode/DebugModeBanner";
import { AppSummaryWidget } from "./summary/AppSummaryWidget";
import { DateRangePicker } from "./DateRangePicker";
import { LazyLoad } from "@components/LazyLoad";
import { useDatePicker } from "@hooks/use-datepicker";
import { NewAppWidget } from "./summary/NewAppWidget";

Component.displayName = "HomePage";
export function Component() {
  const { apps } = useApps();

  const { buildMode } = useApps();
  const [period] = useDatePicker();

  useEffect(() => {
    trackEvent("home_viewed", { period, apps_count: apps.length });
  }, [period, apps]);

  if (apps.length === 0) {
    return (
      <Page title="Welcome">
        <LonelyState />
      </Page>
    );
  }

  return (
    <Page title="Home">
      <div className="flex justify-between items-center">
        <PageHeading title="Home" />
        <div className="flex items-center space-x-2">
          <BuildModeSelector />
          <DateRangePicker />
        </div>
      </div>
      {buildMode === "debug" && <DebugModeBanner />}
      <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-6 mt-8">
        {apps.map((app) => (
          <LazyLoad className="h-36" key={app.id}>
            <AppSummaryWidget app={app} buildMode={buildMode} />
          </LazyLoad>
        ))}
        <LazyLoad className="h-36">
          <NewAppWidget />
        </LazyLoad>
      </div>
    </Page>
  );
}
