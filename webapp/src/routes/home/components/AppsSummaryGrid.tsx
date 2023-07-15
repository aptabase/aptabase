import { AppConfigMenu, Application, useApps } from "@app/apps";
import { DateRangePicker, LazyLoad, PageHeading } from "@app/primitives";
import { DebugModeBanner } from "@app/routes/app/components/DebugModeBanner";
import { AppSummaryWidget } from "@app/widgets";

type Props = {
  apps: Application[];
};

export function AppsSummaryGrid(props: Props) {
  const { buildMode } = useApps();

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
          <LazyLoad className="h-40" key={app.id}>
            <AppSummaryWidget app={app} buildMode={buildMode} />
          </LazyLoad>
        ))}
      </div>
    </>
  );
}
