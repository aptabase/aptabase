import { Page, PageHeading } from "@components/Page";
import { useQuery } from "@tanstack/react-query";
import { liveSessionDetails } from "@features/analytics/query";
import { useApps, useCurrentApp } from "@features/apps";
import { Navigate, useParams } from "react-router-dom";
import { trackEvent } from "@aptabase/web";
import { useEffect } from "react";
import { PingSignal } from "@components/PingSignal";
import { SessionTimeline } from "./liveview/timeline";
import { SessionHeaderCard } from "./liveview/SessionHeaderCard";

Component.displayName = "LiveSessionDetailsPage";
export function Component() {
  const { buildMode } = useApps();
  const app = useCurrentApp();
  const { sessionId } = useParams();

  if (!app || !sessionId) return <Navigate to="/" />;

  const { isLoading, data } = useQuery(
    ["live-session-details", app.id, buildMode, sessionId],
    () => liveSessionDetails({ appId: app.id, buildMode, sessionId }),
    { refetchInterval: 10000 }
  );

  useEffect(() => {
    trackEvent("liveview_session_viewed");
  }, []);

  const aside = () => {
    if (isLoading) return null;
    return <PingSignal color="success" size="xs" />;
  };

  return (
    <Page title="Live View">
      <div className="flex justify-between items-center">
        <PageHeading title="Session Timeline" subtitle={sessionId} />
      </div>

      {data && (
        <div className="mt-10 flex flex-col">
          <SessionHeaderCard appId={app.id} session={data} />

          <div className="mt-10 flex flex-col gap-4">
            <SessionTimeline {...data} />
          </div>
        </div>
      )}
    </Page>
  );
}
