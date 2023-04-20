import clsx from "clsx";

type Props = {
  className?: string;
  children: React.ReactNode;
};

export function Card(props: Props) {
  return <div className={clsx("flex flex-col min-h-[28rem] rounded border border-default p-4", props.className)}>{props.children}</div>;
}
