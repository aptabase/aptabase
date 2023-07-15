import { IconArrowDownRight, IconArrowUpRight } from "@tabler/icons-react";

type Props = {
  values: number[];
};

export function calculateGrowth(arr: number[]): number {
  const half = Math.ceil(arr.length / 2);

  const firstHalf = arr.slice(0, half).reduce((a, b) => a + b, 0);
  const secondHalf = arr.slice(half).reduce((a, b) => a + b, 0);
  const growth = 1 - firstHalf / secondHalf;

  if (isNaN(growth) || growth === -Infinity || growth === Infinity) return 0;

  return growth;
}

export function GrowthIndicator(props: Props) {
  const growth = calculateGrowth(props.values);
  if (growth === 0) return null;

  const isUp = growth > 0;
  const isDown = growth < 0;

  const growthColor = isUp ? "text-success" : "text-destructive";

  return (
    <span className={`text-sm flex font-medium items-center ${growthColor}`}>
      {isUp && <IconArrowUpRight className="h-4 w-4" />}
      {isDown && <IconArrowDownRight className="h-4 w-4" />}
      <span>{Math.floor(growth * 100)}%</span>
    </span>
  );
}
