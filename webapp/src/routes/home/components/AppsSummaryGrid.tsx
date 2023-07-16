import { AppConfigMenu, Application, useApps } from "@app/apps";
import { DateRangePicker, LazyLoad, PageHeading } from "@app/primitives";
import { DebugModeBanner } from "@app/routes/app/components/DebugModeBanner";
import { AppSummaryWidget } from "@app/widgets";
import { trackEvent } from "@aptabase/web";
import { useEffect } from "react";
import { useSearchParams } from "react-router-dom";

type Props = {
  apps: Application[];
};

export function AppsSummaryGrid(props: Props) {
  const { buildMode } = useApps();
  const [searchParams] = useSearchParams();
  const period = searchParams.get("period") || "";

  useEffect(() => {
    trackEvent("home_viewed", { period });
  }, [period]);

  return (
    <>
      <div className="flex justify-between items-end">
        <PageHeading
          title="Your Apps"
          subtitle="Select an app to get started"
        />
        <div className="flex items-center">
          <AppConfigMenu />
          <DateRangePicker />
        </div>
      </div>
      {buildMode === "debug" && <DebugModeBanner />}
      <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-6 mt-8">
        {props.apps.map((app) => (
          <LazyLoad className="h-48" key={app.id}>
            <AppSummaryWidget app={app} buildMode={buildMode} />
          </LazyLoad>
        ))}
      </div>
    </>
  );
}
