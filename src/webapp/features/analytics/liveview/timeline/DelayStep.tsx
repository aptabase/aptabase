import { formatNumber } from "@fns/format-number";
import { IconClock } from "@tabler/icons-react";

type Props = {
  seconds: number;
};

export function DelayStep(props: Props) {
  return (
    <div className="relative">
      <span className="absolute left-4 top-4 -ml-px h-full w-0.5 bg-border" aria-hidden="true"></span>
      <div className="relative flex items-center space-x-2">
        <IconClock className="p-1.5 h-8 w-8 bg-muted border rounded-full text-sucess" />
        <span className="text-xs">{formatNumber(props.seconds, "duration")} later</span>
      </div>
    </div>
  );
}
