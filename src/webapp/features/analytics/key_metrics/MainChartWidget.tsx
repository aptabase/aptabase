import { trackEvent } from "@aptabase/web";
import { useApps } from "@features/apps";
import { formatPeriod } from "@fns/format-date";
import { formatNumber } from "@fns/format-number";
import { useDatePicker } from "@hooks/use-datepicker";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Granularity, periodicStats } from "../query";
import { KeyMetrics } from "./KeyMetrics";
import { MetricsChart } from "./MetricsChart";

type Props = {
  appId: string;
  appName: string;
};

function TooltipContent(props: {
  granularity: Granularity;
  label: string;
  points: Array<{
    name: string;
    value: number;
  }>;
}) {
  return (
    <div className="text-sm whitespace-nowrap">
      <p className="text-center text-muted-foreground">{formatPeriod(props.granularity, props.label)}</p>
      {props.points.map((point) => (
        <p key={point.name}>
          <span className="font-medium">{formatNumber(point.value)}</span>{" "}
          {point.value === 1 ? point.name.toLowerCase().slice(0, -1) : point.name.toLowerCase()}
        </p>
      ))}
    </div>
  );
}

export function MainChartWidget(props: Props) {
  const { buildMode } = useApps();
  const [searchParams] = useSearchParams();
  const [keyMetricToShow, setKeyMetricToShow] = useState<"users" | "sessions" | "events">("users");

  const { startDate, endDate, granularity } = useDatePicker();
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  // useEffect(() => {
  //   const { startDate, endDate, granularity } = mapStartEndDateToQueryParams(startEndDate);
  //   console.log("startDate", startDate);
  //   console.log("endDate", endDate);
  //   console.log("granularity", granularity);
  // }, [startEndDate]);

  const { isLoading, isError, data, refetch } = useQuery({
    queryKey: [
      "periodic-stats",
      buildMode,
      props.appId,
      startDate,
      endDate,
      countryCode,
      appVersion,
      eventName,
      osName,
    ],
    queryFn: () =>
      periodicStats({
        buildMode,
        appId: props.appId,
        startDate: startDate?.toISOString(),
        endDate: endDate?.toISOString(),
        granularity,
        countryCode,
        appVersion,
        eventName,
        osName,
      }),
    staleTime: 10000,
  });

  useEffect(() => {
    trackEvent("dashboard_viewed", {
      startDate: startDate?.toISOString(),
      endDate: endDate?.toISOString(),
      name: props.appName,
    });
  }, [startDate, endDate, props.appName]);

  const users: number[] = [],
    sessions: number[] = [],
    events: number[] = [],
    labels: string[] = [];

  data?.forEach((x) => {
    users.push(x.users);
    sessions.push(x.sessions);
    events.push(x.events);
    labels.push(x.period);
  });
  const total = sessions.reduce((a, b) => a + b, 0);

  return (
    <>
      <KeyMetrics activeMetric={keyMetricToShow} onChangeActiveMetric={setKeyMetricToShow} {...props} />
      <MetricsChart
        isEmpty={total === 0}
        activeMetric={keyMetricToShow}
        isError={isError}
        isLoading={isLoading}
        hasPartialData={false} // TODO fix: period !== "last-month"
        users={users}
        sessions={sessions}
        events={events}
        granularity={granularity}
        labels={labels}
        formatLabel={(label) => formatPeriod(granularity, label.toString())}
        renderTooltip={({ label, points }) => (
          <TooltipContent granularity={granularity} label={label} points={points} />
        )}
        refetch={refetch}
      />
    </>
  );
}
