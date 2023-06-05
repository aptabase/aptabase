import clsx from "clsx";
import { useRef } from "react";
import { useLazyLoad } from "./useLazyLoad";

type Props = {
  className?: string;
  children: React.ReactNode;
};

export function Card(props: Props) {
  const ref: any = useRef<HTMLDivElement>();
  const show: boolean = useLazyLoad<HTMLDivElement>(ref);

  return (
    <div
      ref={ref}
      className={clsx(
        "flex flex-col min-h-[28rem] bg-default p-4",
        props.className
      )}
    >
      {show && props.children}
    </div>
  );
}
