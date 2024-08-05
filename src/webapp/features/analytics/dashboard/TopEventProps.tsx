import { useApps } from "@features/apps";
import { useQuery } from "@tanstack/react-query";
import { useAtomValue } from "jotai";
import { Fragment, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { dateFilterValuesAtom } from "../../../atoms/date-atoms";
import { topEventProps } from "../query";
import { TopNChart } from "./TopNChart";
import { TopNTitle } from "./TopNTitle";

type Props = {
  appId: string;
};

type AggregateValueName = "events" | "sum" | "median" | "min" | "max";

export function TopEventProps(props: Props) {
  const { buildMode } = useApps();
  const [searchParams] = useSearchParams();
  const { startDateIso, endDateIso, granularity } = useAtomValue(dateFilterValuesAtom);

  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const [stringKeyIndex, setStringKeyIndex] = useState(0);
  const [selectedNumericKey, setSelectedNumericKey] = useState<[AggregateValueName, string | undefined]>([
    "events",
    undefined,
  ]);

  const {
    isLoading,
    isError,
    data: rows,
    refetch,
  } = useQuery({
    queryKey: [
      "top-event-props",
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
      topEventProps({
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

  const stringKeys = [...new Set((rows || []).map((row) => row.stringKey).filter((x) => !!x))];

  const numericKeys = [...new Set((rows || []).map((row) => row.numericKey).filter((x) => !!x))];

  if (!isLoading && stringKeys.length === 0) {
    const value =
      (rows || []).find((x) => selectedNumericKey[0] === "events" || x.numericKey === selectedNumericKey[1])?.[
        selectedNumericKey[0]
      ] ?? 0;

    return (
      <>
        <div className="flex justify-between">
          <TopNTitle backProperty="eventName">{eventName}</TopNTitle>
          <NumericKeySelector numericKeys={numericKeys} onChange={setSelectedNumericKey} />
        </div>
        <div className="flex justify-center items-center mt-20">
          <span className="text-8xl text-center">{value}</span>
        </div>
      </>
    );
  }

  let items = (rows || [])
    .filter(
      (x) =>
        x.stringKey === stringKeys[stringKeyIndex] &&
        (selectedNumericKey[0] === "events" || x.numericKey === selectedNumericKey[1])
    )
    .map((row) => ({
      name: (row.stringValue ? row.stringValue : "n/a") + (row.numericKey ? ` (${row.numericKey}) ` : ""),
      value: row[selectedNumericKey[0]],
      key: [row.stringValue, row.numericKey, row.stringKey].join("-"),
    }))
    .sort((a, b) => b.value - a.value);

  /// When we have multiple numeric events, we need to dedupe the string props
  /// when the "Events" value is selected, because the query returns it multiple times
  if (numericKeys.length >= 2 && selectedNumericKey[0] === "events") {
    const deduped = items.reduce((acc, item) => {
      acc[item.name] = { value: item.value, key: item.key };
      // acc[item.name] = item.value;
      return acc;
    }, {} as Record<string, { value: number; key: string }>);

    items = Object.keys(deduped).map((name) => ({ name, value: deduped[name].value, key: deduped[name].key }));
  }

  // Having the key set to "events" will cause it to inherit settings from the EventWidget
  // While setting to "props" will have the default settings
  const key = selectedNumericKey[0] === "events" ? "events" : "props";

  return (
    <TopNChart
      id={key}
      key={key}
      title={<TopNTitle backProperty="eventName">{eventName}</TopNTitle>}
      keyLabel={<StringKeySelector stringKeys={stringKeys} onChangeIndex={setStringKeyIndex} />}
      valueLabel={
        numericKeys.length === 0 ? (
          "Events"
        ) : (
          <NumericKeySelector numericKeys={numericKeys} onChange={setSelectedNumericKey} />
        )
      }
      defaultFormat="absolute"
      isLoading={isLoading}
      isError={isError}
      items={items}
      refetch={refetch}
    />
  );
}

function StringKeySelector(props: { stringKeys: string[]; onChangeIndex: (idx: number) => void }) {
  if (props.stringKeys.length === 0) {
    return null;
  }

  return (
    <select
      className="form-select compact min-w-[6rem]"
      defaultValue={0}
      onChange={(e) => props.onChangeIndex(e.currentTarget.selectedIndex)}
    >
      {props.stringKeys.map((key) => (
        <option key={key} value={key}>
          {key}
        </option>
      ))}
    </select>
  );
}

function NumericKeySelector(props: {
  numericKeys: string[];
  onChange: (value: [AggregateValueName, string | undefined]) => void;
}) {
  const onChangeValue = (e: React.ChangeEvent<HTMLSelectElement>) => {
    if (e.currentTarget.value === "") {
      props.onChange(["events", undefined]);
    } else {
      const parts = e.currentTarget.value.split(",");
      props.onChange([parts[0] as AggregateValueName, parts[1]]);
    }
  };

  return (
    <select className="form-select compact min-w-[6rem]" onChange={onChangeValue}>
      <option value={[]}>Events</option>
      {props.numericKeys.map((key) => (
        <Fragment key={key}>
          <option disabled>---</option>
          <option value={["median", key]}>Median ({key})</option>
          <option value={["min", key]}>Min ({key})</option>
          <option value={["max", key]}>Max ({key})</option>
          <option value={["sum", key]}>Sum ({key})</option>
        </Fragment>
      ))}
    </select>
  );
}
