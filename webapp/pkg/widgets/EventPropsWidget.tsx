import { TopNChart } from "./charts";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { CardTitle } from "./CardTitle";
import { topEventProps } from "./query";
import { useApps } from "@app/apps";

type Props = {
  appId: string;
};

export function EventPropsWidget(props: Props) {
  const { buildMode } = useApps();
  const [searchParams] = useSearchParams();
  const period = searchParams.get("period") || "";
  const countryCode = searchParams.get("countryCode") || "";
  const appVersion = searchParams.get("appVersion") || "";
  const eventName = searchParams.get("eventName") || "";
  const osName = searchParams.get("osName") || "";

  const [index, setIndex] = useState(0);

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

  const keys = [...new Set((rows || []).map((row) => row.key))];

  const items = (rows || [])
    .filter((x) => x.key === keys[index])
    .map((row) => ({ name: row.value, value: row.events }));

  return (
    <TopNChart
      title={<CardTitle backProperty="eventName">{eventName}</CardTitle>}
      labels={[keys[index], "Events"]}
      renderLabel={() =>
        keys.length > 0 && (
          <select
            className="form-select compact min-w-[6rem]"
            defaultValue={0}
            onChange={(e) => setIndex(e.currentTarget.selectedIndex)}
          >
            {keys.map((key, i) => (
              <option key={key} value={i}>
                {key}
              </option>
            ))}
          </select>
        )
      }
      isLoading={isLoading}
      isError={isError}
      items={items}
    />
  );
}
