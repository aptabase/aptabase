import { GrowthIndicator } from "@components/GrowthIndicator";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@components/Tooltip";
import { formatNumber } from "@fns/format-number";
import { twJoin, twMerge } from "tailwind-merge";

type Props = {
  label: string;
  current: number;
  previous?: number;
  active?: boolean;
  activeClassName?: string;
  onClick?: VoidFunction;
  format: "duration" | "number";
};

export function Metric(props: Props) {
  const Container = props.onClick ? "button" : "div";

  const TooltipOnOverflow = (
    <TooltipProvider delayDuration={0}>
      <Tooltip>
        <TooltipContent>{props.current}</TooltipContent>
        <TooltipTrigger>{formatNumber(props.current, props.format)}</TooltipTrigger>
      </Tooltip>
    </TooltipProvider>
  );

  return (
    <Container
      className={twJoin(
        "inline-flex flex-col text-center rounded p-2 min-w-[7rem]",
        props.onClick && "hover:bg-accent"
      )}
      onClick={props.onClick}
    >
      <div className="text-2xl font-semibold w-full">
        {props.format === "number" && props.current >= 1e3
          ? TooltipOnOverflow
          : formatNumber(props.current, props.format)}
      </div>
      <div className="text-sm text-muted-foreground w-full">
        {props.label}{" "}
        <div className={twMerge("ml-1 p-1 inline-block rounded", props.activeClassName, !props.active && "hidden")} />
      </div>
      <GrowthIndicator
        className="mt-1 w-full"
        current={props.current}
        previous={props.previous}
        previousFormatted={`${formatNumber(props.previous ?? 0, props.format)} ${props.label.toLowerCase()}`}
      />
    </Container>
  );
}
