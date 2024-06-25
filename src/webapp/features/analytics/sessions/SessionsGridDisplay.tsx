import { CountryFlag, CountryName } from "@features/geo";
import { formatDate, formatTime } from "@fns/format-date";
import { formatNumber } from "@fns/format-number";
import { IconClick, IconClock, IconDeviceDesktop, IconDevices, IconMap, IconUser } from "@tabler/icons-react";
import { useNavigate } from "react-router-dom";
import { OSIcon } from "../dashboard/icons/os";
import { LiveRecentSession } from "../query";

export type SessionStartEndDateFilters = {
  startDate?: string;
  endDate?: string;
  sessionId?: string;
};

type Props = {
  appId: string;
  sessions: LiveRecentSession[];
  sessionFilters?: SessionStartEndDateFilters;
};

export function SessionsGridDisplay(props: Props) {
  const navigate = useNavigate();
  const sessions = props.sessions;

  const handleClick = (session: LiveRecentSession) => () => {
    navigate(`/${props.appId}/live/${session.id}`, { state: { sessionFilters: props.sessionFilters } });
  };

  return (
    <div className="flow-root">
      <div className="overflow-x-auto">
        <div className="inline-block min-w-full py-2 align-middle">
          <table className="min-w-full divide-y divide-gray-300">
            <thead>
              <tr>
                <th scope="col" className="py-3.5 pl-4 pr-3 text-left text-sm font-semibold sm:pl-6 lg:pl-8">
                  <div className="flex items-center gap-2">
                    <IconUser className="text-muted-foreground h-5 w-5" />
                    Timestamp
                  </div>
                </th>
                <th scope="col" className="px-3 py-3.5 text-left text-sm font-semibold">
                  <div className="flex items-center gap-2">
                    <IconClock className="text-muted-foreground h-5 w-5" />
                    Duration
                  </div>
                </th>
                <th scope="col" className="px-3 py-3.5 text-left text-sm font-semibold">
                  <div className="flex items-center gap-2">
                    <IconClick className="text-muted-foreground h-5 w-5" />
                    Events
                  </div>
                </th>
                <th scope="col" className="px-3 py-3.5 text-left text-sm font-semibold">
                  <div className="flex items-center gap-2">
                    <IconMap className="text-muted-foreground h-5 w-5" />
                    Country
                  </div>
                </th>
                <th scope="col" className="relative py-3.5 pl-3 pr-4 sm:pr-6 lg:pr-8">
                  <div className="flex items-center gap-2">
                    <IconDeviceDesktop className="text-muted-foreground h-5 w-5" />
                    OS
                  </div>
                </th>
                <th scope="col" className="px-3 py-3.5 text-left text-sm font-semibold">
                  <div className="flex items-center gap-2">
                    <IconDevices className="text-muted-foreground h-5 w-5" />
                    App Version
                  </div>
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-600">
              {sessions.map((session) => (
                <tr key={session.id} onClick={handleClick(session)} className="hover:bg-accent cursor-pointer">
                  <td className="whitespace-nowrap py-4 pl-4 pr-3 text-sm sm:pl-6 lg:pl-8">
                    {`${formatDate(session.startedAt)} ${formatTime(session.startedAt)}`}
                  </td>
                  <td className="whitespace-nowrap px-3 py-4 text-sm">
                    <div className="flex justify-end">{formatNumber(session.duration, "duration")}</div>
                  </td>
                  <td className="whitespace-nowrap px-3 py-4 text-sm">
                    <div className="flex justify-center">{session.eventsCount}</div>
                  </td>
                  <td className="whitespace-nowrap px-3 py-4 text-sm">
                    <div className="flex items-center gap-2">
                      <CountryFlag countryCode={session.countryCode} />
                      <div>
                        {session.regionName && <span>{session.regionName} · </span>}
                        <CountryName countryCode={session.countryCode} />
                      </div>
                    </div>
                  </td>
                  <td className="whitespace-nowrap px-3 py-4 text-sm">
                    <div className="flex items-center gap-2">
                      <span>
                        {session.osName} {session.osVersion}
                      </span>
                      <OSIcon name={session.osName} className="h-5 w-5" />
                    </div>
                  </td>
                  <td className="whitespace-nowrap px-3 py-4 text-sm">{session.appVersion}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
