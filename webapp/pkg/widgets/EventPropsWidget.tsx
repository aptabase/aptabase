import { TopNChart } from "./charts";
import { useQuery } from "@tanstack/react-query";
import { Fragment, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { CardTitle } from "./CardTitle";
import { topEventProps } from "./query";
import { useApps } from "@app/apps";

type Props = {
  appId: string;
};

type AggregateValueName = "events" | "sum" | "median" | "min" | "max";

export function EventPropsWidget(props: Props) {
  const { buildMode } = useApps();
  const [searchParams] = useSearchParams();
  const period = searchParams.get("period") || "";
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const [stringKeyIndex, setStringKeyIndex] = useState(0);
  const [selectedNumericKey, setSelectedNumericKey] = useState<
    [AggregateValueName, string | undefined]
  >(["events", undefined]);

  const {
    isLoading,
    isError,
    data: rows,
  } = useQuery(
    [
      "top-event-props",
      buildMode,
      props.appId,
      period,
      countryCode,
      appVersion,
      eventName,
      osName,
    ],
    () =>
      topEventProps({
        buildMode,
        appId: props.appId,
        period,
        countryCode,
        appVersion,
        eventName,
        osName,
      })
  );

  const stringKeys = [
    ...new Set((rows || []).map((row) => row.stringKey).filter((x) => !!x)),
  ];

  const numericKeys = [
    ...new Set((rows || []).map((row) => row.numericKey).filter((x) => !!x)),
  ];

  if (!isLoading && stringKeys.length === 0) {
    const value =
      (rows || []).find(
        (x) =>
          selectedNumericKey[0] === "events" ||
          x.numericKey === selectedNumericKey[1]
      )?.[selectedNumericKey[0]] ?? 0;

    return (
      <>
        <div className="flex justify-between">
          <CardTitle backProperty="eventName">{eventName}</CardTitle>
          <NumericKeySelector
            numericKeys={numericKeys}
            onChange={setSelectedNumericKey}
          />
        </div>
        <div className="flex justify-center items-center mt-20">
          <span className="text-8xl text-center">{value}</span>
        </div>
      </>
    );
  }

  const items = (rows || [])
    .filter(
      (x) =>
        x.stringKey === stringKeys[stringKeyIndex] &&
        (selectedNumericKey[0] === "events" ||
          x.numericKey === selectedNumericKey[1])
    )
    .map((row) => ({
      name: row.stringValue,
      value: row[selectedNumericKey[0]],
    }))
    .sort((a, b) => b.value - a.value);

  return (
    <TopNChart
      title={<CardTitle backProperty="eventName">{eventName}</CardTitle>}
      labels={[stringKeys[stringKeyIndex], "Events"]}
      renderKeyLabel={
        <StringKeySelector
          stringKeys={stringKeys}
          onChangeIndex={setStringKeyIndex}
        />
      }
      renderValueLabel={
        <NumericKeySelector
          numericKeys={numericKeys}
          onChange={setSelectedNumericKey}
        />
      }
      isLoading={isLoading}
      isError={isError}
      items={items}
    />
  );
}

function StringKeySelector(props: {
  stringKeys: string[];
  onChangeIndex: (idx: number) => void;
}) {
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
  if (props.numericKeys.length === 0) {
    return <>Events</>;
  }

  const onChangeValue = (e: React.ChangeEvent<HTMLSelectElement>) => {
    if (e.currentTarget.value === "") {
      props.onChange(["events", undefined]);
    } else {
      const parts = e.currentTarget.value.split(",");
      props.onChange([parts[0] as AggregateValueName, parts[1]]);
    }
  };

  return (
    <select
      className="form-select compact min-w-[6rem]"
      onChange={onChangeValue}
    >
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
