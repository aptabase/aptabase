import clsx from "clsx";

type Props = {
  label: string;
  value: string;
  active?: boolean;
  activeClassName?: string;
  onClick?: VoidFunction;
};

export function Metric(props: Props) {
  const Container = props.onClick ? "button" : "div";

  return (
    <Container
      className={clsx("flex flex-col py-2 px-4 rounded min-w-[6rem]", {
        "hover:bg-gray-100": props.onClick,
      })}
      onClick={props.onClick}
    >
      <div className="text-2xl font-semibold">{props.value}</div>
      <div className="text-sm text-secondary">
        {props.label}{" "}
        <div
          className={clsx("p-1 inline-block rounded", props.activeClassName, {
            invisible: !props.active,
          })}
        />
      </div>
    </Container>
  );
}
