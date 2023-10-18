import { formatDate, formatTime } from "@fns/format-date";
import { IconUser } from "@tabler/icons-react";
import { EventPropsList } from "./EventPropsList";

type Props = {
  eventName: string;
  timestamp: Date;
  eventStringProps?: string;
  eventNumericProps?: string;
};

export function FirstEventStep(props: Props) {
  return (
    <div className="relative">
      <span className="absolute left-4 top-4 -ml-px h-full w-0.5 bg-border" />
      <div className="relative flex items-center space-x-2">
        <IconUser className="p-1.5 h-8 w-8 bg-muted border rounded-full" />
        <span className="text-lg">{props.eventName}</span>
      </div>
      <div className="text-xs ml-10 text-muted-foreground">
        {formatDate(props.timestamp)} {formatTime(props.timestamp)}
      </div>

      <EventPropsList numericProps={props.eventNumericProps} stringProps={props.eventStringProps} />
    </div>
  );
}
