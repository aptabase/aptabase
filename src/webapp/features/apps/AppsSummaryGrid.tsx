import { AppConfigMenu, Application, useApps } from "@features/apps";
import { DateRangePicker, LazyLoad, PageHeading } from "@features/primitives";
import { AppSummaryWidget } from "@features/widgets";
import { trackEvent } from "@aptabase/web";
import { useEffect } from "react";
import { useSearchParams } from "react-router-dom";
import { DebugModeBanner } from "./DebugModeBanner";

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
      <div className="flex justify-between items-center">
        <PageHeading title="My Apps" />
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
