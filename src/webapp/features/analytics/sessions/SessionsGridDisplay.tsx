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
  isLoading: boolean;
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
        <div className="inline-block min-w-full py-2 align-middle relative">
          {props.isLoading && (
            <div className="absolute w-full h-full bg-black bg-opacity-50 z-10">
              <div className="flex items-center justify-center h-full">
                <svg
                  className="animate-spin h-8 w-8 inline fill-foreground"
                  xmlns="http://www.w3.org/2000/svg"
                  viewBox="0 0 100 101"
                  fill="none"
                >
                  <path
                    d="M93.9676 39.0409C96.393 38.4038 97.8624 35.9116 97.0079 33.5539C95.2932 28.8227 92.871 24.3692 89.8167 20.348C85.8452 15.1192 80.8826 10.7238 75.2124 7.41289C69.5422 4.10194 63.2754 1.94025 56.7698 1.05124C51.7666 0.367541 46.6976 0.446843 41.7345 1.27873C39.2613 1.69328 37.813 4.19778 38.4501 6.62326C39.0873 9.04874 41.5694 10.4717 44.0505 10.1071C47.8511 9.54855 51.7191 9.52689 55.5402 10.0491C60.8642 10.7766 65.9928 12.5457 70.6331 15.2552C75.2735 17.9648 79.3347 21.5619 82.5849 25.841C84.9175 28.9121 86.7997 32.2913 88.1811 35.8758C89.083 38.2158 91.5421 39.6781 93.9676 39.0409Z"
                    fill="currentFill"
                  />
                </svg>
              </div>
            </div>
          )}
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
            <tbody className="divide-y divide-gray-600 min-h-screen">
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
