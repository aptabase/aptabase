import clsx from "clsx";
import { LazyLoad } from "./LazyLoad";

type Props = {
  className?: string;
  children: React.ReactNode;
};

export function Card(props: Props) {
  return (
    <LazyLoad
      className={clsx(
        "min-h-[12rem] bg-background py-4 sm:px-4",
        props.className
      )}
    >
      {props.children}
    </LazyLoad>
  );
}
