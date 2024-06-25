import { trackEvent } from "@aptabase/web";
import { Page, PageHeading } from "@components/Page";
import { UserSessionsList } from "@features/analytics/sessions/UsersSessionsList";
import { useApps, useCurrentApp } from "@features/apps";
import { useEffect } from "react";
import { Navigate } from "react-router-dom";
import { BuildModeSelector } from "./mode/BuildModeSelector";
import { DebugModeBanner } from "./mode/DebugModeBanner";

Component.displayName = "UserSessionsPage";
export function Component() {
  const app = useCurrentApp();
  const { buildMode } = useApps();

  if (!app) return <Navigate to="/" />;

  useEffect(() => {
    trackEvent("user_sessions_viewed");
  }, []);

  return (
    <Page title="User Sessions">
      {buildMode === "debug" && <DebugModeBanner />}
      <div className="flex justify-between items-center">
        <PageHeading title="User Sessions" subtitle="Historical list of sessions" />
        <div className="flex items-center">
          <BuildModeSelector />
        </div>
      </div>

      <UserSessionsList appId={app.id} buildMode={buildMode} />
    </Page>
  );
}
