import { Application, AppIcon } from "@features/apps";
import { Link } from "react-router-dom";
import { GrowthIndicator } from "@components/GrowthIndicator";
import { SummaryDataContainer } from "./SummaryDataContainer";
import { EmptyStateWidget } from "./EmptyStateWidget";
import { AppLockedContent } from "../locked/AppLockedContent";
import { WaitingForEventsInfo } from "./WaitingForEventsInfo";
import { DailyUsersChart } from "./DailyUsersChart";
import { useDatePicker } from "@hooks/use-datepicker";

type Props = {
  app: Application;
  buildMode: "debug" | "release";
};

export function AppSummaryWidget(props: Props) {
  const [period] = useDatePicker();
  const params = period ? `?period=${period}` : "";

  if (props.app.lockReason) {
    return (
      <EmptyStateWidget app={props.app}>
        <AppLockedContent reason={props.app.lockReason} />
      </EmptyStateWidget>
    );
  }

  if (!props.app.hasEvents) {
    return (
      <EmptyStateWidget app={props.app}>
        <WaitingForEventsInfo />
      </EmptyStateWidget>
    );
  }

  return (
    <Link
      to={`/${props.app.id}/${params}`}
      className="border cursor-pointer rounded-t-lg shadow bg-card hover:bg-muted h-full"
    >
      <SummaryDataContainer appId={props.app.id} buildMode={props.buildMode} period={period}>
        {({ dailyUsers, metrics }) => (
          <>
            <div className="px-3 py-2 h-20">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-2 truncate">
                  <AppIcon className="w-6 h-6" iconPath={props.app.iconPath} />
                  <span className="truncate">{props.app.name}</span>
                </div>
                {metrics ? (
                  <div className="flex items-center space-x-2">
                    <GrowthIndicator
                      current={metrics.current.dailyUsers}
                      previous={metrics.previous?.dailyUsers}
                      previousFormatted={`${metrics.previous?.dailyUsers.toFixed(0)} daily users`}
                    />
                    <span className="text-2xl">{metrics?.current.dailyUsers.toFixed(0)}</span>
                  </div>
                ) : null}
              </div>
              <div>{metrics ? <p className="text-sm text-muted-foreground text-right">daily users</p> : null}</div>
            </div>
            <div className="h-28">
              <DailyUsersChart values={dailyUsers ?? []} />
            </div>
          </>
        )}
      </SummaryDataContainer>
    </Link>
  );
}
