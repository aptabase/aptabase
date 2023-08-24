import { twMerge } from "tailwind-merge";

type Props = {
  size?: "normal" | "small";
};

export function WaitingSignal(props: Props) {
  const size = props.size ?? "normal";

  const pulseClassName = size === "normal" ? "h-6 w-6" : "h-4 w-4";
  const circleClassName = size === "normal" ? "h-2.5 w-2.5" : "h-2 w-2";

  return (
    <span
      className={twMerge(
        "relative flex h-6 w-6 mx-auto items-center justify-center",
        pulseClassName
      )}
    >
      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-success opacity-75"></span>
      <span
        className={twMerge(
          "relative inline-flex rounded-full h-2.5 w-2.5 bg-success",
          circleClassName
        )}
      ></span>
    </span>
  );
}
