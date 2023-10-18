import { PingSignal } from "@components/PingSignal";

export function WaitingEventsStep() {
  return (
    <div className="relative">
      <div className="relative flex items-center space-x-2">
        <div className="p-1.5">
          <PingSignal color="success" />
        </div>
        <span className="text-xs text-muted-foreground">Waiting for more events...</span>
      </div>
    </div>
  );
}
