import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@features/primitives";

export function DebugModeBanner() {
  return (
    <div className="fixed top-12 z-20 lg:top-0 left-0 lg:left-64 right-0 mb-4">
      <div className="w-full border-t-4 border-t-warning text-center text-xs">
        <div className="inline-block -mt-2 text-warning-foreground px-2 rounded-b-md bg-warning">
          <TooltipProvider>
            <Tooltip delayDuration={0}>
              <TooltipTrigger>
                <span>Debug Data</span>
              </TooltipTrigger>
              <TooltipContent>
                <p>
                  The dashboard is showing data from apps built for development.
                </p>
                <p>Switch to release to see production data.</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        </div>
      </div>
    </div>
  );
}
