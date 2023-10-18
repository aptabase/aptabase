import { formatDate, formatTime } from "@fns/format-date";
import { IconClick } from "@tabler/icons-react";
import { EventPropsList } from "./EventPropsList";
import { formatNumber } from "@fns/format-number";

type Props = {
  eventName: string;
  timestamp: Date;
  eventStringProps?: string;
  eventNumericProps?: string;
  distance: number;
};

export function EventStep(props: Props) {
  return (
    <div className="relative">
      <span className="absolute left-4 top-4 -ml-px h-full w-0.5 bg-border" />
      <div className="relative flex items-center space-x-2">
        <IconClick className="p-1.5 h-8 w-8 bg-muted border rounded-full" />
        <div>
          <div
            className="text-xs text-muted-foreground"
            title={`${formatDate(props.timestamp)} ${formatTime(props.timestamp)}`}
          >
            {props.distance > 60000
              ? `${formatDate(props.timestamp)} ${formatTime(props.timestamp)}`
              : props.distance > 0
              ? `${formatNumber(props.distance / 1000, "duration")} later`
              : ""}
          </div>
          <span className="text-lg">{props.eventName}</span>
        </div>
      </div>

      <EventPropsList numericProps={props.eventNumericProps} stringProps={props.eventStringProps} />
    </div>
  );
}
