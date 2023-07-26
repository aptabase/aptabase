import { Application, AppIcon } from "../apps";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { keyMetrics, periodicStats } from "./query";
import { NumbersChart } from "./NumbersChart";
import { GrowthIndicator } from "./GrowthIndicator";

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
      })
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
      }).then((s) => s.rows.map((x) => x.sessions))
  );

  const params = period ? `?period=${period}` : "";

  return (
    <Link
      to={`/${props.app.id}/${params}`}
      key={props.app.id}
      className="border cursor-pointer rounded shadow hover:bg-muted"
    >
      <div className="p-2">
        <div className="flex items-center justify-between h-6">
          <div className="flex items-center space-x-2 truncate">
            <AppIcon className="w-6 h-6" iconPath={props.app.iconPath} />
            <span className="truncate">{props.app.name}</span>
          </div>
          {metrics ? (
            <div className="flex items-center space-x-2">
              <GrowthIndicator
                current={metrics.current.sessions}
                previous={metrics.previous?.sessions}
                previousFormatted={`${metrics.previous?.sessions} sessions`}
              />
              <span className="text-2xl">{metrics?.current.sessions}</span>
            </div>
          ) : null}
        </div>
        <div className="h-6">
          {metrics ? (
            <p className="text-sm text-muted-foreground text-right">sessions</p>
          ) : null}
        </div>
      </div>
      <div className="h-32">
        <NumbersChart values={values ?? []} />
      </div>
    </Link>
  );
}
