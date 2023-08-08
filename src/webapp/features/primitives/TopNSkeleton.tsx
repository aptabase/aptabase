import { useDelay } from "@features/primitives";

export function TopNSkeleton() {
  const show = useDelay(400);

  if (!show) return null;

  return (
    <div className="animate-pulse flex space-x-4 mt-2">
      <div className="flex-1 grid grid-cols-6 gap-2">
        <div className="h-8 bg-accent rounded col-span-4"></div>
        <div className="h-8 col-span-1"></div>
        <div className="h-8 bg-accent rounded col-span-1"></div>

        <div className="h-8 bg-accent rounded col-span-3"></div>
        <div className="h-8 col-span-2"></div>
        <div className="h-8 bg-accent rounded col-span-1"></div>

        <div className="h-8 bg-accent rounded col-span-3"></div>
        <div className="h-8 col-span-2"></div>
        <div className="h-8 bg-accent rounded col-span-1"></div>

        <div className="h-8 bg-accent rounded col-span-1"></div>
        <div className="h-8 col-span-4"></div>
        <div className="h-8 bg-accent rounded col-span-1"></div>
      </div>
    </div>
  );
}
