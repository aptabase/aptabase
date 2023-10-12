import { OSIcon } from "@features/analytics/dashboard/icons/os";
import { CountryFlag, CountryName } from "@features/geo";
import { formatTime } from "@fns/format-date";
import { formatNumber } from "@fns/format-number";
import { IconClick, IconClock, IconUser } from "@tabler/icons-react";

type Props = {
  sessionId: string;
  eventsCount: number;
  startedAt: Date;
  duration: number;
  countryCode: string;
  region: string;
  osName: string;
  osVersion: string;
};

export function SessionHeaderCard(props: Props) {
  const shortSessionId = props.sessionId.split("-")?.[0] ?? props.sessionId;

  return (
    <div className="text-sm flex flex-col gap-1 p-2 rounded hover:bg-muted hover:cursor-pointer">
      <div className="flex items-center gap-4">
        <div className="text-lg min-w-[5rem]">{shortSessionId}</div>
        <div className="flex items-center gap-1">
          <IconClick className="text-muted-foreground h-4 w-4" />
          <span>{props.eventsCount} events</span>
        </div>
      </div>
      <div className="flex items-center gap-4">
        <div className="flex items-center gap-1 min-w-[5rem]">
          <IconUser className="text-muted-foreground h-4 w-4" />
          <span className="tabular-nums">
            <span>{formatTime(props.startedAt)}</span>
          </span>
        </div>
        <div className="flex items-center gap-1">
          <IconClock className="text-muted-foreground h-4 w-4" />
          <span>{formatNumber(props.duration, "duration")}</span>
        </div>
      </div>
      <div className="flex gap-1 items-center">
        <CountryFlag countryCode={props.countryCode} size="sm" />
        <div>
          {props.region && <span>{props.region} · </span>} <CountryName countryCode={props.countryCode} />
        </div>
      </div>
      <div className="flex gap-1 items-center">
        <OSIcon name={props.osName} className="h-4 w-4" />
        <div>
          {props.osName} {props.osVersion}
        </div>
      </div>
    </div>
  );
}
