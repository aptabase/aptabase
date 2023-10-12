import { Page, PageHeading } from "@components/Page";
import { WorldMap } from "./liveview/world/WorldMap";
import { DevelopmentNotice } from "./liveview/DevelopmentNotice";
import { PingSignal } from "@components/PingSignal";
import { useQuery } from "@tanstack/react-query";
import { liveGeoDataPoints } from "@features/analytics/query";
import { useApps, useCurrentApp } from "@features/apps";
import { Navigate } from "react-router-dom";
import { AppConfigMenu } from "./mode/AppConfigMenu";
import { trackEvent } from "@aptabase/web";
import { useEffect } from "react";

Component.displayName = "LiveViewPage";
export function Component() {
  const { buildMode } = useApps();
  const app = useCurrentApp();

  if (!app) return <Navigate to="/" />;

  const { isLoading, data: dataPoints } = useQuery(
    ["live-geo", app.id, buildMode],
    () => liveGeoDataPoints({ appId: app.id, buildMode }),
    { refetchInterval: 10000 }
  );

  useEffect(() => {
    trackEvent("liveview_viewed");
  }, []);

  const totalUsers = dataPoints?.reduce((total, point) => total + point.users, 0) ?? 0;

  const subtitle = () => {
    if (isLoading) return "";
    if (totalUsers === 0) return "No users in the last hour";
    if (totalUsers === 1) return "1 user in the last hour";
    return `${totalUsers} users in the last hour`;
  };

  const aside = () => {
    if (isLoading) return <></>;
    return <PingSignal color="success" size="xs" />;
  };

  return (
    <Page title="Live View">
      <div className="flex justify-between items-center">
        <PageHeading title="Live View" aside={aside()} subtitle={subtitle()} />
        <div className="flex items-center">
          <AppConfigMenu />
        </div>
      </div>

      <div className="py-10 md:py-0 md:p-10 flex items-center justify-center">
        <WorldMap className="md:h-[30rem]" points={dataPoints || []} />
      </div>

      <div className="mt-10">
        <DevelopmentNotice />
      </div>
    </Page>
  );
}