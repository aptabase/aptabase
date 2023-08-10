import { formatNumber } from "@features/primitives";
import { trackEvent } from "@aptabase/web";
import { useQuery } from "@tanstack/react-query";
import { format, parseJSON } from "date-fns";
import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Granularity, periodicStats } from "../query";
import { KeyMetrics } from "./KeyMetrics";
import { useApps } from "@features/apps";
import { hourCycle } from "@features/env";
import { MetricsChart } from "./MetricsChart";

type Props = {
  appId: string;
};

const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

function formatPeriod(granularity: Granularity, period: string) {
  try {
    if (granularity === "hour") {
      return format(parseJSON(period), hourCycle === "h12" ? "haaaaa'm'" : "HH:mm");
    }

    const [year, month, day] = period.substring(0, 10).split("-");
    const monthName = months[parseInt(month, 10) - 1];

    switch (granularity) {
      case "day":
        return `${monthName} ${day}`;
      case "month":
        return `${monthName} ${year}`;
    }
  } catch (e) {
    return period;
  }
}

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
      <p className="text-center text-muted-foreground">
        {formatPeriod(props.granularity, props.label)}
      </p>
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
  const [keyMetricToShow, setKeyMetricToShow] = useState<"users" | "sessions">("users");
  const [showEvents, setShowEvents] = useState(false);

  const toggleShowEvents = () => setShowEvents((x) => !x);

  const period = searchParams.get("period") || "";
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const { isLoading, isError, data } = useQuery(
    ["periodic-stats", buildMode, props.appId, period, countryCode, appVersion, eventName, osName],
    () =>
      periodicStats({
        buildMode,
        appId: props.appId,
        period,
        countryCode,
        appVersion,
        eventName,
        osName,
      }),
    { staleTime: 10000 }
  );

  useEffect(() => {
    trackEvent("dashboard_viewed", { period });
  }, [period]);

  // TODO: make this more efficient, we don't need to map over the data multiple times
  const users = (data?.rows || []).map((x) => x.users);
  const sessions = (data?.rows || []).map((x) => x.sessions);
  const events = (data?.rows || []).map((x) => x.events);
  const labels = (data?.rows || []).map((x) => x.period);
  const total = sessions.reduce((a, b) => a + b, 0);

  const granularity = data?.granularity || "day";
  return (
    <>
      <KeyMetrics
        activeMetric={keyMetricToShow}
        onChangeActiveMetric={setKeyMetricToShow}
        showEvents={showEvents}
        onToggleShowEvents={toggleShowEvents}
        {...props}
      />
      <MetricsChart
        isEmpty={total === 0}
        activeMetric={keyMetricToShow}
        showEvents={showEvents}
        isError={isError}
        isLoading={isLoading}
        hasPartialData={period !== "last-month"}
        users={users}
        sessions={sessions}
        events={events}
        granularity={granularity}
        labels={labels}
        formatLabel={(label) => formatPeriod(granularity, label.toString())}
        renderTooltip={({ label, points }) => (
          <TooltipContent granularity={granularity} label={label} points={points} />
        )}
      />
    </>
  );
}
