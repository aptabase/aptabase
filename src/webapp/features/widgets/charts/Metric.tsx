import clsx from "clsx";
import { GrowthIndicator } from "../GrowthIndicator";
import { formatNumber } from "../../primitives";

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

  return (
    <Container
      className={clsx(
        "inline-flex flex-col text-center rounded p-2 min-w-[7rem]",
        props.onClick ? "hover:bg-accent" : null
      )}
      onClick={props.onClick}
    >
      <div className="text-2xl font-semibold w-full">
        {formatNumber(props.current, props.format)}
      </div>
      <div className="text-sm text-muted-foreground w-full">
        {props.label}{" "}
        <div
          className={clsx("ml-1 p-1 inline-block rounded", props.activeClassName, {
            hidden: !props.active,
          })}
        />
      </div>
      <GrowthIndicator
        className="mt-1 w-full"
        current={props.current}
        previous={props.previous}
        previousFormatted={`${formatNumber(
          props.previous ?? 0,
          props.format
        )} ${props.label.toLowerCase()}`}
      />
    </Container>
  );
}
