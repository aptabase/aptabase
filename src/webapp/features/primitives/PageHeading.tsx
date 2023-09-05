import { twJoin } from "tailwind-merge";

type Props = {
  title: string;
  subtitle?: string;
  onClick?: VoidFunction;
};

export function PageHeading(props: Props) {
  return (
    <div>
      <h1
        className={twJoin("text-2xl font-medium", !!props.onClick && "cursor-pointer")}
        onClick={props.onClick}
      >
        {props.title}
      </h1>
      {props.subtitle && <p className="text-muted-foreground">{props.subtitle}</p>}
    </div>
  );
}
