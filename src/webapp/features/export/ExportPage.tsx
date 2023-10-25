import { Page, PageHeading } from "@components/Page";
import { useApps, useCurrentApp } from "@features/apps";
import { Navigate } from "react-router-dom";
import { BuildModeSelector } from "@features/analytics/mode/BuildModeSelector";
import { DebugModeBanner } from "@features/analytics/mode/DebugModeBanner";
import { ExportPageBody } from "./ExportPageBody";

Component.displayName = "ExportPage";
export function Component() {
  const app = useCurrentApp();
  const { buildMode } = useApps();

  if (!app) return <Navigate to="/" />;

  return (
    <Page title="Export">
      {buildMode === "debug" && <DebugModeBanner />}

      <div className="flex justify-between items-center">
        <PageHeading title="Export" subtitle="Freedom to explore your data" />
        <div className="flex items-center">
          <BuildModeSelector />
        </div>
      </div>

      <div className="max-w-3xl mt-8">
        <ExportPageBody app={app} buildMode={buildMode} />
      </div>
    </Page>
  );
}
