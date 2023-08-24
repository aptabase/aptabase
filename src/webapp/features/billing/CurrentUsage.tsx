import { PingSignal } from "@features/primitives";
import { BillingInfo } from "./useBilling";
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
  billing: BillingInfo;
};

const getUsageBarColor = (perc: number) => {
  return perc >= 100 ? "bg-destructive" : perc >= 90 ? "bg-warning" : "bg-primary";
};

export function CurrentUsage(props: Props) {
  const perc = (props.billing.usage / props.billing.plan.monthlyEvents) * 100;

  return (
    <div className="flex flex-col h-full justify-between">
      <div className="flex items-center mb-1 justify-between">
        <span>Usage</span>
        <span className="text-sm text-muted-foreground">
          {months[props.billing.month - 1]} / {props.billing.year}
        </span>
      </div>
      <div className="space-y-1">
        <div className="text-sm flex items-center space-x-1">
          <span>{props.billing.usage.toLocaleString()}</span>
          <span className="text-muted-foreground">
            / {props.billing.plan.monthlyEvents.toLocaleString()} events ({perc.toFixed(1)}
            %)
          </span>
          {props.billing.state === "OVERUSE" && <PingSignal color="warning" />}
        </div>
        <div className="overflow-hidden rounded bg-accent">
          <div
            className={twMerge("h-2 rounded", getUsageBarColor(perc))}
            style={{ width: `${perc}%` }}
          />
        </div>
      </div>
    </div>
  );
}
