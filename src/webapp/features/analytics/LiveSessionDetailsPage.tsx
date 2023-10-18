import { Page, PageHeading } from "@components/Page";
import { useQuery } from "@tanstack/react-query";
import { liveSessionDetails } from "@features/analytics/query";
import { useApps, useCurrentApp } from "@features/apps";
import { Navigate, useParams } from "react-router-dom";
import { trackEvent } from "@aptabase/web";
import { useEffect } from "react";
import { SessionTimeline } from "./liveview/timeline";
import { IconClick, IconClock } from "@tabler/icons-react";
import { formatNumber } from "@fns/format-number";
import { CountryFlag, CountryName } from "@features/geo";
import { OSIcon } from "./dashboard/icons/os";

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

  return (
    <Page title="Live View">
      <div className="flex justify-between items-center">
        <PageHeading title="Session Timeline" subtitle={sessionId} />
      </div>

      {data && (
        <div className="mt-10 flex flex-col">
          <div className=" grid grid-cols-2 space-y-1 max-w-[16rem]">
            <div className="flex items-center gap-2">
              <IconClick className="text-muted-foreground h-5 w-5" />
              <span>{data.eventsCount} events</span>
            </div>

            <div className="flex gap-2 items-center">
              <CountryFlag countryCode={data.countryCode} />
              <div>
                {data.regionName && <span>{data.regionName} · </span>} <CountryName countryCode={data.countryCode} />
              </div>
            </div>

            <div className="flex gap-2 items-center">
              <IconClock className="text-muted-foreground h-5 w-5" />
              <span>{formatNumber(data.duration, "duration")}</span>
            </div>

            <div className="flex gap-2 items-center">
              <OSIcon name={data.osName} className="h-5 w-5" />
              <span>
                {data.osName} {data.osVersion}
              </span>
            </div>
          </div>

          <div className="mt-8 flex flex-col gap-4">
            <SessionTimeline {...data} />
          </div>
        </div>
      )}
    </Page>
  );
}
