import { OSIcon } from "@features/analytics/dashboard/icons/os";
import { CountryFlag, CountryName } from "@features/geo";
import { formatTime } from "@fns/format-date";
import { formatNumber } from "@fns/format-number";
import { IconClick, IconClock, IconDevices, IconUser } from "@tabler/icons-react";
import { LiveRecentSession } from "../query";

type Props = {
  appId: string;
  session: LiveRecentSession;
};

export function SessionHeaderCard(props: Props) {
  const { eventsCount, startedAt, duration, appVersion, countryCode, osName, osVersion, regionName } = props.session;
  const shortSessionId = props.session.id.slice(-8);

  return (
    <div className="text-sm flex flex-col gap-1 p-2">
      <div className="flex items-center gap-4">
        <div className="text-lg min-w-[6rem]">{shortSessionId}</div>
      </div>
      <div className="flex items-center gap-4">
        <div className="flex items-center gap-1 min-w-[8rem]">
          <IconDevices className="text-muted-foreground h-4 w-4" />
          <span>Version {appVersion}</span>
        </div>
        <div className="flex items-center gap-1">
          <IconClick className="text-muted-foreground h-4 w-4" />
          <span>{eventsCount} events</span>
        </div>
      </div>
      <div className="flex items-center gap-4">
        <div className="flex items-center gap-1 min-w-[8rem]">
          <IconUser className="text-muted-foreground h-4 w-4" />
          <span className="tabular-nums">{formatTime(startedAt)}</span>
        </div>
        <div className="flex items-center gap-1">
          <IconClock className="text-muted-foreground h-4 w-4" />
          <span className="tabular-nums">{formatNumber(duration, "duration")}</span>
        </div>
      </div>
      <div className="flex gap-1 items-center">
        <CountryFlag countryCode={countryCode} size="sm" />
        <div>
          {regionName && <span>{regionName} · </span>} <CountryName countryCode={countryCode} />
        </div>
      </div>
      <div className="flex gap-1 items-center">
        <OSIcon name={osName} className="h-4 w-4" />
        <div>
          {osName} {osVersion}
        </div>
      </div>
    </div>
  );
}
