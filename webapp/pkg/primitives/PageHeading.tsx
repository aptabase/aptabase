import clsx from "clsx";

type Props = {
  title: string;
  subtitle?: string;
  onClick?: VoidFunction;
};

export function PageHeading(props: Props) {
  return (
    <div>
      <h1
        className={clsx("text-2xl font-medium", {
          "cursor-pointer": !!props.onClick,
        })}
        onClick={props.onClick}
      >
        {props.title}
      </h1>
      {props.subtitle && <p className="text-subtle">{props.subtitle}</p>}
    </div>
  );
}
