import { Application, AppIcon } from "@features/apps";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { keyMetrics, periodicStats } from "../query";
import { NumbersChart } from "./NumbersChart";
import { GrowthIndicator } from "@features/primitives";

type Props = {
  app: Application;
  buildMode: "debug" | "release";
};

export function AppSummaryWidget(props: Props) {
  const [searchParams] = useSearchParams();
  const period = searchParams.get("period") || "";

  const { data: metrics } = useQuery(
    ["key-metrics", props.app.id, props.buildMode, period],
    () =>
      keyMetrics({
        buildMode: props.buildMode,
        appId: props.app.id,
        period,
        countryCode: "",
        appVersion: "",
        eventName: "",
        osName: "",
      }),
    { staleTime: 60000 }
  );

  const { data: values } = useQuery(
    ["periodic-stats", props.app.id, props.buildMode, period],
    () =>
      periodicStats({
        buildMode: props.buildMode,
        appId: props.app.id,
        period,
        countryCode: "",
        appVersion: "",
        eventName: "",
        osName: "",
      }).then((s) => s.rows.map((x) => x.users)),
    { staleTime: 60000 }
  );

  const params = period ? `?period=${period}` : "";

  return (
    <Link
      to={`/${props.app.id}/${params}`}
      className="border cursor-pointer rounded shadow hover:bg-muted h-full"
    >
      <div className="p-2 h-16">
        <div className="flex items-start justify-between">
          <div className="flex items-center space-x-2 truncate">
            <AppIcon className="w-6 h-6" iconPath={props.app.iconPath} />
            <span className="truncate">{props.app.name}</span>
          </div>
          {metrics ? (
            <div className="flex items-center space-x-2">
              <GrowthIndicator
                current={metrics.current.dailyUsers}
                previous={metrics.previous?.dailyUsers}
                previousFormatted={`${metrics.previous?.dailyUsers.toFixed(0)} avg. daily users`}
              />
              <span className="text-2xl">{metrics?.current.dailyUsers.toFixed(0)}</span>
            </div>
          ) : null}
        </div>
        <div>
          {metrics ? (
            <p className="text-sm text-muted-foreground text-right">avg. daily users</p>
          ) : null}
        </div>
      </div>
      <div className="h-32">
        <NumbersChart values={values ?? []} />
      </div>
    </Link>
  );
}
