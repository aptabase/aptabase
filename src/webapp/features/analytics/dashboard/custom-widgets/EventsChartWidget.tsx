import { trackEvent } from "@aptabase/web";
import { Button } from "@components/Button";
import { EventNameFilterDropdown } from "@features/analytics/sessions/filters/EventNameFilterDropdown";
import { useApps } from "@features/apps";
import { formatPeriod } from "@fns/format-date";
import { formatNumber } from "@fns/format-number";
import { useQuery } from "@tanstack/react-query";
import { useAtom, useAtomValue } from "jotai";
import { useEffect, useMemo, useState } from "react";
import { dateFilterValuesAtom } from "../../../../atoms/date-atoms";
import { dashboardWidgetsAtom, EventsChartWidgetConfig, SingleWidgetConfig } from "../../../../atoms/widgets-atoms";
import { Granularity, periodicStats } from "../../query";
import { EventsChart, EventSeries } from "./EventsChart";

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

const emptyDefaultEventsChartWidgetConfig: SingleWidgetConfig<EventsChartWidgetConfig> = {
  id: "events-chart",
  isMinimized: false,
  orderIndex: 0,
  properties: {
    selectedEventNames: [],
  },
};

export function EventsChartWidget(props: Props) {
  const { buildMode } = useApps();
  const [widgetsConfig, setWidgetsConfig] = useAtom(dashboardWidgetsAtom);
  const eventsChartWidgetConfig = useMemo(
    () => widgetsConfig.find((w) => w.id === "events-chart") ?? emptyDefaultEventsChartWidgetConfig,
    [widgetsConfig]
  );
  const [dropdownsApplied, setDropdownsApplied] = useState<number>(
    (eventsChartWidgetConfig.properties?.selectedEventNames?.filter(Boolean).length ?? 0) + 1
  );
  const eventsChartSeriesConfig: string[] = eventsChartWidgetConfig.properties?.selectedEventNames ?? [];
  const { startDateIso, endDateIso, granularity } = useAtomValue(dateFilterValuesAtom);

  const [eventSeries, setEventSeries] = useState<EventSeries[]>([]);

  const addEventLine = (eventName: string | undefined, eventNameIndex: number) => {
    const eventsChart = [...(eventsChartWidgetConfig.properties?.selectedEventNames ?? [])];
    if (eventName) {
      eventsChart[eventNameIndex] = eventName;
    } else {
      eventsChart.splice(eventNameIndex, 1);
    }
    setWidgetsConfig({
      widgetId: eventsChartWidgetConfig.id,
      type: "update-properties",
      properties: {
        selectedEventNames: eventsChart,
      },
    });
  };

  const {
    isLoading,
    isError,
    data: visibleData,
    refetch,
  } = useQuery({
    queryKey: [
      "periodic-stats",
      buildMode,
      props.appId,
      startDateIso,
      endDateIso,
      JSON.stringify(eventsChartSeriesConfig),
    ],
    queryFn: () =>
      Promise.all(
        eventsChartSeriesConfig.map((eventName) =>
          periodicStats({
            buildMode,
            appId: props.appId,
            startDate: startDateIso,
            endDate: endDateIso,
            granularity,
            eventName,
          })
        )
      ),
    staleTime: 10000,
    enabled: !!startDateIso && !!endDateIso && !!granularity && eventsChartSeriesConfig.length > 0,
  });

  useEffect(() => {
    if (!visibleData) return;

    if (eventsChartSeriesConfig.length === 0) {
      setEventSeries([]);
      return;
    }

    const processedDataArrays = visibleData.map((data) => {
      let processedData = data;
      // If the start date matches start of date we are filtering by 'All time'
      if (startDateIso === new Date(0).toISOString() && processedData.length) {
        const firstItemWithData = processedData.findIndex((x) => !!x.events || !!x.sessions || !!x.users);
        const sliceDataFrom = firstItemWithData > 2 ? firstItemWithData - 2 : Math.max(0, firstItemWithData);
        processedData = processedData.slice(sliceDataFrom);
      }
      return processedData;
    });

    setEventSeries(
      eventsChartSeriesConfig.map((eventName, index) => ({
        name: eventName,
        values: processedDataArrays[index].map((x) => x.events),
      }))
    );
  }, [visibleData, eventsChartSeriesConfig, startDateIso]);

  useEffect(() => {
    if (!startDateIso || !endDateIso) return;

    trackEvent("custom_events_chart_viewed", {
      startDate: startDateIso,
      endDate: endDateIso,
      name: props.appName,
    });
  }, [startDateIso, endDateIso, props.appName]);

  const total = useMemo(
    () =>
      (visibleData ?? []).reduce(
        (sum, dataArray) => sum + dataArray.reduce((innerSum, item) => innerSum + item.events, 0),
        0
      ),
    [visibleData]
  );

  const labels = useMemo(() => (visibleData?.[0] ?? []).map((x) => x.period), [visibleData]);

  return (
    <>
      <div className="flex flex-wrap gap-2 items-center pb-5">
        {Array.from({ length: dropdownsApplied }).map((_, index) => (
          <EventNameFilterDropdown
            key={index}
            appId={props.appId}
            onValueChange={(newEventName) => addEventLine(newEventName, index)}
            localValue={eventsChartSeriesConfig[index]}
            localPersistence
            onRemove={() => setDropdownsApplied((prev) => prev - 1)}
            className="w-[200px]"
          />
        ))}
        <Button
          onClick={() => setDropdownsApplied((prev) => prev + 1)}
          variant="outline"
          size="icon"
          className="rounded-full w-8 h-8"
        >
          +
        </Button>
      </div>
      <EventsChart
        isEmpty={total === 0}
        isError={isError}
        isLoading={isLoading}
        hasPartialData={false}
        granularity={granularity}
        series={eventSeries}
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
