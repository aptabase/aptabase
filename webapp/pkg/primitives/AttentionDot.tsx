import { cn } from "./utils";

type Props = {
  className?: string;
};

export function AttentionDot(props: Props) {
  return null;

  return (
    <span
      className={cn(
        "relative flex items-center justify-center h-3 w-3",
        props.className
      )}
    >
      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-warning opacity-75"></span>
      <span className="relative inline-flex rounded-full h-2 w-2 bg-warning"></span>
    </span>
  );
}
