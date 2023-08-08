import clsx from "clsx";
import { LazyLoad } from "./LazyLoad";

type Props = {
  children: React.ReactNode;
};

export function Card(props: Props) {
  return (
    <LazyLoad className={"min-h-[12rem] bg-background py-4 sm:px-4"}>{props.children}</LazyLoad>
  );
}
