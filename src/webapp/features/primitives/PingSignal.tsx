import { twMerge } from "tailwind-merge";

type Props = {
  color: "success" | "warning";
  size?: "base" | "sm";
};

const pulseSizes = {
  base: "h-5 w-5",
  sm: "h-4 w-4",
};

const circleSizes = {
  base: "h-2.5 w-2.5",
  sm: "h-2 w-2",
};

export function PingSignal(props: Props) {
  const size = props.size ?? "base";

  const colorClassName = props.color === "success" ? "bg-success" : "bg-warning";

  return (
    <span
      className={twMerge("relative flex h-6 w-6 items-center justify-center", pulseSizes[size])}
    >
      <span
        className={twMerge(
          "animate-ping absolute inline-flex h-full w-full rounded-full opacity-75",
          colorClassName
        )}
      ></span>
      <span
        className={twMerge(
          "relative inline-flex rounded-full h-2.5 w-2.5",
          colorClassName,
          circleSizes[size]
        )}
      ></span>
    </span>
  );
}
