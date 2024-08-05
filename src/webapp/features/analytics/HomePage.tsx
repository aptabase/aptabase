import { trackEvent } from "@aptabase/web";
import { LazyLoad } from "@components/LazyLoad";
import { Page, PageHeading } from "@components/Page";
import { useApps } from "@features/apps";
import { useAtomValue } from "jotai";
import { useEffect } from "react";
import { dateFilterValuesAtom } from "../../atoms/date-atoms";
import { DateFilterContainer } from "./date-filters/DateFilterContainer";
import { LonelyState } from "./LonelyState";
import { BuildModeSelector } from "./mode/BuildModeSelector";
import { DebugModeBanner } from "./mode/DebugModeBanner";
import { AppSummaryWidget } from "./summary/AppSummaryWidget";
import { NewAppWidget } from "./summary/NewAppWidget";

Component.displayName = "HomePage";
export function Component() {
  const { apps, buildMode } = useApps();
  const { startDateIso, endDateIso } = useAtomValue(dateFilterValuesAtom);

  useEffect(() => {
    if (!startDateIso || !endDateIso) {
      return;
    }

    trackEvent("home_viewed", {
      startDate: startDateIso,
      endDate: endDateIso,
      apps_count: apps.length,
    });
  }, [startDateIso, endDateIso, apps]);

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
        <div className="flex items-end space-x-2">
          <BuildModeSelector />
          <DateFilterContainer />
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
