import { PingSignal } from "@components/PingSignal";

export function WaitingForEventsInfo() {
  return (
    <>
      <PingSignal color="success" size="sm" />
      <div>
        <p className="text-center">Waiting for the first event...</p>
        <p className="text-center text-muted-foreground">Click to learn more</p>
      </div>
    </>
  );
}
