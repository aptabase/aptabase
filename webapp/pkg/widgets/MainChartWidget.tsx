import { BarChart, Metric } from "@app/charts";
import { Card } from "@app/primitives";
import { trackEvent } from "@aptabase/web";
import { useQuery } from "@tanstack/react-query";
import { format, formatDuration, intervalToDuration, parseJSON } from "date-fns";
import { useEffect } from "react";
import { useSearchParams } from "react-router-dom";
import { Granularity, keyMetrics, periodicStats } from "./query";

type Props = {
  appId: string;
};

function KeyMetrics(props: Props) {
  const [searchParams] = useSearchParams();
  const period = searchParams.get("period") || "";
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const {
    isLoading,
    isError,
    data: metrics,
  } = useQuery(["key-metrics", props.appId, period, countryCode, appVersion, eventName, osName], () =>
    keyMetrics({ appId: props.appId, period, countryCode, appVersion, eventName, osName })
  );

  const formatDistanceLocale: Record<string, string> = { xSeconds: "{{count}}s", xMinutes: "{{count}}m", xHours: "{{count}}h" };
  const shortEnLocale: Locale = { formatDistance: (token, count) => formatDistanceLocale[token].replace("{{count}}", count) };

  const duration = formatDuration(intervalToDuration({ start: 0, end: (metrics?.durationSeconds || 0) * 1000 }), {
    format: ["hours", "minutes", "seconds"],
    locale: shortEnLocale,
  });

  return (
    <div className="flex justify-between sm:justify-start sm:divide-x sm:space-x-8 sm:divide-gray-200 mb-8">
      {!isLoading && !isError && (
        <>
          <Metric label="Sessions" value={metrics?.sessions.toString() || "0"} />
          <Metric label="Avg. Duration" value={duration || "0s"} />
          <Metric label="Events" value={metrics?.events.toString() || "0"} />
        </>
      )}
    </div>
  );
}

const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

function formatPeriod(granularity: Granularity, period: string) {
  try {
    if (granularity === "hour") {
      return format(parseJSON(period), "haaaaa'm'");
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

function TooltipContent(props: { granularity: Granularity; value: number; label: string }) {
  return (
    <div className="text-sm">
      <p className="text-center text-secondary">{formatPeriod(props.granularity, props.label)}</p>
      <p>
        <span className="font-medium">{props.value}</span> {props.value === 1 ? "session" : "sessions"}
      </p>
    </div>
  );
}

export function MainChartWidget(props: Props) {
  const [searchParams] = useSearchParams();
  const period = searchParams.get("period") || "";
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const { isLoading, isError, data } = useQuery(["periodic-stats", props.appId, period, countryCode, appVersion, eventName, osName], () =>
    periodicStats({ appId: props.appId, period, countryCode, appVersion, eventName, osName })
  );

  useEffect(() => {
    trackEvent("dashboard_viewed", { period });
  }, [period]);

  const values = (data?.rows || []).map((x) => x.sessions);
  const labels = (data?.rows || []).map((x) => x.period);
  const total = values.reduce((a, b) => a + b, 0);

  const granularity = data?.granularity || "day";
  return (
    <Card>
      <KeyMetrics {...props} />
      <BarChart
        category="Sessions"
        isEmpty={total === 0}
        isError={isError}
        isLoading={isLoading}
        hasPartialData={period !== "last-month"}
        values={values}
        showAllLabels={granularity === "month"}
        labels={labels}
        formatLabel={(label) => formatPeriod(granularity, label.toString())}
        renderTooltip={({ value, label }) => <TooltipContent granularity={granularity} value={value} label={label} />}
      />
    </Card>
  );
}
