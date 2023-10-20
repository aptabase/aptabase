import { Page, PageHeading } from "@components/Page";
import { useApps } from "@features/apps";
import { LonelyState } from "./LonelyState";
import { trackEvent } from "@aptabase/web";
import { useEffect } from "react";
import { AppConfigMenu } from "./mode/AppConfigMenu";
import { DebugModeBanner } from "./mode/DebugModeBanner";
import { AppSummaryWidget } from "./summary/AppSummaryWidget";
import { DateRangePicker } from "./DateRangePicker";
import { LazyLoad } from "@components/LazyLoad";
import { useDatePicker } from "@hooks/use-datepicker";

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
          <AppConfigMenu />
          <DateRangePicker />
        </div>
      </div>
      {buildMode === "debug" && <DebugModeBanner />}
      <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-6 mt-8">
        {apps.map((app) => (
          <LazyLoad className="h-48" key={app.id}>
            <AppSummaryWidget app={app} buildMode={buildMode} />
          </LazyLoad>
        ))}
      </div>
    </Page>
  );
}
