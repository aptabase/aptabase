import { useQuery } from "@tanstack/react-query";
import { LiveRecentSession, liveRecentSessions } from "../query";
import { SessionHeaderCard } from "./SessionHeaderCard";
import { useNavigate } from "react-router-dom";

type Props = {
  appId: string;
  buildMode: "release" | "debug";
};

export function RecentSessionsList(props: Props) {
  const navigate = useNavigate();
  const { data } = useQuery(
    ["live-sessions", props.appId, props.buildMode],
    () => liveRecentSessions({ appId: props.appId, buildMode: props.buildMode }),
    { refetchInterval: 10000 }
  );

  const sessions = data || [];

  if (sessions.length === 0) return null;

  const handleClick = (session: LiveRecentSession) => () => {
    navigate(`/${props.appId}/live/${session.id}`);
  };

  return (
    <div>
      <p className="font-title text-xl">Recent Sessions</p>
      <p className="mb-4 text-muted-foreground text-sm">Most active sessions in the last hour.</p>

      <div className="grid sm:grid-cols-2 md:grid-cols-3 gap-4">
        {sessions.map((s) => (
          <div key={s.id} className="rounded hover:bg-muted hover:cursor-pointer" onClick={handleClick(s)}>
            <SessionHeaderCard appId={props.appId} session={s} />
          </div>
        ))}
      </div>
    </div>
  );
}
