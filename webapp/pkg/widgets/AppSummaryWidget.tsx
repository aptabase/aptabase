import { Application, AppIcon } from "@app/apps";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { periodicStats } from "./query";
import { NumbersChart } from "./NumbersChart";
import { GrowthIndicator } from "./GrowthIndicator";

type Props = {
  app: Application;
  buildMode: "debug" | "release";
};

export function AppSummaryWidget(props: Props) {
  const [searchParams] = useSearchParams();
  const period = searchParams.get("period") || "";

  const { data } = useQuery(
    ["daily-sessions", props.app.id, props.buildMode, period],
    () =>
      periodicStats({
        buildMode: props.buildMode,
        appId: props.app.id,
        period,
        countryCode: "",
        appVersion: "",
        eventName: "",
        osName: "",
      })
  );

  const params = period ? `?period=${period}` : "";
  const values = data?.rows.map((x) => x.sessions) ?? [];

  return (
    <Link
      to={`/${props.app.id}/${params}`}
      key={props.app.id}
      className="border rounded shadow hover:bg-muted"
    >
      <div className="flex items-center p-4 justify-between">
        <div className="flex items-center space-x-2 truncate">
          <AppIcon className="w-6 h-6" iconPath={props.app.iconPath} />
          <span className="truncate">{props.app.name}</span>
        </div>
        {false && <GrowthIndicator values={values} />}
      </div>

      <div className="h-28">
        <NumbersChart values={values} />
      </div>
    </Link>
  );
}
