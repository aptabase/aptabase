import { useQuery } from "@tanstack/react-query";
import { liveRecentSessions } from "../query";
import { SessionHeaderCard } from "./SessionHeaderCard";

type Props = {
  appId: string;
  buildMode: "release" | "debug";
};

export function RecentSessionsList(props: Props) {
  const { data } = useQuery(
    ["live-sessions", props.appId, props.buildMode],
    () => liveRecentSessions({ appId: props.appId, buildMode: props.buildMode }),
    { refetchInterval: 10000 }
  );

  const sessions = data || [];

  if (sessions.length === 0) return null;

  return (
    <div>
      <p className="font-title text-xl">Recent Sessions</p>
      <p className="mb-4 text-muted-foreground text-sm">Most active sessions in the last hour.</p>

      <div className="grid sm:grid-cols-2 md:grid-cols-3 gap-4">
        {sessions.map((s) => (
          <SessionHeaderCard key={s.id} session={s} />
        ))}
      </div>
    </div>
  );
}
