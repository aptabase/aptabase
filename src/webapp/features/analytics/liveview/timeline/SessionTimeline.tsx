import { Fragment } from "react";
import { DelayStep } from "./DelayStep";
import { EventStep } from "./EventStep";
import { FirstEventStep } from "./FirstEventStep";
import { WaitingEventsStep } from "./WaitingEventsStep";

type Props = {
  eventsName: string[];
  eventsTimestamp: string[];
  eventsStringProps: string[];
  eventsNumericProps: string[];
};

export function SessionTimeline(props: Props) {
  const timestamps = (props.eventsTimestamp || []).map((ts) => new Date(ts));
  const sortedIndexes = timestamps
    .map((_, idx) => idx)
    .sort((a, b) => timestamps[a].getTime() - timestamps[b].getTime());

  return (
    <>
      {sortedIndexes.map((idx, i) => {
        const ts = timestamps[idx];

        const distance = i === 0 ? 0 : ts.getTime() - timestamps[sortedIndexes[i - 1]].getTime();
        const eventName = props.eventsName?.[idx] ?? "";
        const stringProps = props.eventsStringProps?.[idx];
        const numericProps = props.eventsNumericProps?.[idx];

        return (
          <Fragment key={i}>
            {distance > 60000 && <DelayStep seconds={distance / 1000} />}

            {i === 0 ? (
              <FirstEventStep
                eventName={eventName}
                timestamp={ts}
                eventNumericProps={numericProps}
                eventStringProps={stringProps}
              />
            ) : (
              <EventStep
                eventName={eventName}
                timestamp={ts}
                eventNumericProps={numericProps}
                eventStringProps={stringProps}
                distance={distance}
              />
            )}
          </Fragment>
        );
      })}

      <WaitingEventsStep />
    </>
  );
}
