import { trackEvent } from "@aptabase/web";
import { useApps } from "@features/apps";
import { formatPeriod } from "@fns/format-date";
import { formatNumber } from "@fns/format-number";
import { useQuery } from "@tanstack/react-query";
import { useAtomValue } from "jotai";
import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { dateFilterValuesAtom } from "../../../atoms/date-atoms";
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

  const { startDateIso, endDateIso, granularity } = useAtomValue(dateFilterValuesAtom);
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const { isLoading, isError, data, refetch } = useQuery({
    queryKey: [
      "periodic-stats",
      buildMode,
      props.appId,
      startDateIso,
      endDateIso,
      countryCode,
      appVersion,
      eventName,
      osName,
    ],
    queryFn: () =>
      periodicStats({
        buildMode,
        appId: props.appId,
        startDate: startDateIso,
        endDate: endDateIso,
        granularity,
        countryCode,
        appVersion,
        eventName,
        osName,
      }),
    staleTime: 10000,
    enabled: !!startDateIso && !!endDateIso && !!granularity,
  });

  useEffect(() => {
    if (!startDateIso || !endDateIso) {
      return;
    }
    trackEvent("dashboard_viewed", {
      startDate: startDateIso,
      endDate: endDateIso,
      name: props.appName,
    });
  }, [startDateIso, endDateIso, props.appName]);

  const users: number[] = [],
    sessions: number[] = [],
    events: number[] = [],
    labels: string[] = [];

  let visibleData = data;
  // If the start date matches start of date we are filtering by 'All time'
  // So avoid displaying all samples until the first one that contains data
  if (startDateIso === new Date(0).toISOString() && !!data?.length) {
    const firstItemWithData = data.findIndex((x) => !!x.events || !!x.sessions || !!x.users);
    const sliceDataFrom = firstItemWithData > 2 ? firstItemWithData - 2 : Math.max(0, firstItemWithData);
    visibleData = data.slice(sliceDataFrom);
  }

  visibleData?.forEach((x) => {
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
        // TODO: this should be set whenever our endDate is not Now - some delta (depending on granularity)
        hasPartialData={false}
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
