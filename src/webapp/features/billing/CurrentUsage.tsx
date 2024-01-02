import { PingSignal } from "@components/PingSignal";
import { twMerge } from "tailwind-merge";

const months = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
];

type Props = {
  month: number;
  year: number;
  usage: number;
  quota: number;
  state: "OK" | "OVERUSE";
};

const colors = {
  OK: "bg-primary",
  OVERUSE: "bg-destructive",
};

export function CurrentUsage(props: Props) {
  const percentage = (props.usage / props.quota) * 100;

  return (
    <div className="flex flex-col h-full justify-between">
      <div className="flex items-center mb-1 justify-between">
        <span>Usage</span>
        <span className="text-sm text-muted-foreground">
          {months[props.month - 1]} / {props.year}
        </span>
      </div>
      <div className="space-y-1">
        <div className="text-sm flex items-center space-x-1">
          <span>{props.usage.toLocaleString()}</span>
          <span className="text-muted-foreground">
            / {props.quota.toLocaleString()} events ({percentage.toFixed(2)}
            %)
          </span>
          {props.state === "OVERUSE" && <PingSignal color="warning" size="xs" />}
        </div>
        <div className="overflow-hidden rounded bg-accent">
          <div className={twMerge("h-2 rounded", colors[props.state])} style={{ width: `${percentage}%` }} />
        </div>
      </div>
    </div>
  );
}
