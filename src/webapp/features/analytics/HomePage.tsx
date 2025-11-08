import { trackEvent } from "@aptabase/web";
import { Alert, AlertDescription } from "@components/Alert";
import { Button } from "@components/Button";
import { LazyLoad } from "@components/LazyLoad";
import { Page, PageHeading } from "@components/Page";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@components/Tooltip";
import { useApps } from "@features/apps";
import { AppRequestPurpose, IncomingAppRequest } from "@features/apps/app-requests";
import { OwnershipTransferRequestsModal } from "@features/apps/OwnershipTransferRequestsModal";
import { api } from "@fns/api";
import { IconCrown, IconInfoCircle } from "@tabler/icons-react";
import { useQuery } from "@tanstack/react-query";
import { useAtomValue } from "jotai";
import { useEffect, useState } from "react";
import { dateFilterValuesAtom } from "../../atoms/date-atoms";
import { DateFilterContainer } from "./date-filters/DateFilterContainer";
import { LonelyState } from "./LonelyState";
import { BuildModeSelector } from "./mode/BuildModeSelector";
import { DebugModeBanner } from "./mode/DebugModeBanner";
import { AppSummaryWidget } from "./summary/AppSummaryWidget";
import { NewAppWidget } from "./summary/NewAppWidget";

Component.displayName = "HomePage";
export function Component() {
  const { apps, buildMode } = useApps();
  const { startDateIso, endDateIso } = useAtomValue(dateFilterValuesAtom);
  const [showTransferModal, setShowTransferModal] = useState(false);

  const { data: transferRequests } = useQuery({
    queryKey: ["appRequestsHomePage", AppRequestPurpose.AppOwnership],
    queryFn: () => api.get<IncomingAppRequest[]>(`/_app-requests?purpose=${AppRequestPurpose.AppOwnership}`),
    refetchInterval: 30000, // check every 30 seconds
    refetchOnWindowFocus: true,
  });

  useEffect(() => {
    if (!startDateIso || !endDateIso) {
      return;
    }

    trackEvent("home_viewed", {
      startDate: startDateIso,
      endDate: endDateIso,
      apps_count: apps.length,
    });
  }, [startDateIso, endDateIso, apps]);

  if (apps.length === 0) {
    return (
      <Page title="Welcome">
        <LonelyState />
      </Page>
    );
  }

  const hasPendingRequests = transferRequests && transferRequests.length > 0;

  // split apps into owned and shared
  const ownedApps = apps.filter((app) => app.hasOwnership);
  const sharedApps = apps.filter((app) => !app.hasOwnership);
  const shouldShowSplit = sharedApps.length > 0;

  return (
    <Page title="Home">
      <div className="flex justify-between items-center">
        <PageHeading title="Home" />
        <div className="flex items-end space-x-2">
          <BuildModeSelector />
          <DateFilterContainer />
        </div>
      </div>
      {buildMode === "debug" && <DebugModeBanner />}

      {/* Ownership Transfer Requests Alert */}
      {hasPendingRequests && (
        <div className="mt-6">
          <Alert className="border-warning/20 bg-warning/10">
            <IconCrown className="h-4 w-4 text-warning" />
            <div className="flex items-center justify-between">
              <AlertDescription>
                You have {transferRequests.length} pending ownership transfer{transferRequests.length > 1 ? "s" : ""}{" "}
                waiting for your review.
              </AlertDescription>
              <Button size="sm" variant="default" onClick={() => setShowTransferModal(true)} className="ml-4">
                Review Requests
              </Button>
            </div>
          </Alert>
        </div>
      )}

      {shouldShowSplit ? (
        <div className="space-y-8 mt-8">
          {/* Owned Apps Section - Always visible when there's a split */}
          <div>
            <div className="flex items-center space-x-2 mb-4">
              <h2 className="text-lg font-semibold">Owned by me</h2>
              <TooltipProvider delayDuration={0}>
                <Tooltip>
                  <TooltipContent>The billing plan only applies to apps owned by you</TooltipContent>
                  <TooltipTrigger>
                    <IconInfoCircle className="h-4 w-4 text-muted-foreground" />
                  </TooltipTrigger>
                </Tooltip>
              </TooltipProvider>
            </div>
            <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-6">
              {ownedApps.map((app) => (
                <LazyLoad className="h-36" key={app.id}>
                  <AppSummaryWidget app={app} buildMode={buildMode} />
                </LazyLoad>
              ))}
              <LazyLoad className="h-36">
                <NewAppWidget />
              </LazyLoad>
            </div>
          </div>

          {/* Shared Apps Section */}
          {sharedApps.length > 0 && (
            <div>
              <h2 className="text-lg font-semibold mb-4">Shared with me</h2>
              <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-6">
                {sharedApps.map((app) => (
                  <LazyLoad className="h-36" key={app.id}>
                    <AppSummaryWidget app={app} buildMode={buildMode} />
                  </LazyLoad>
                ))}
              </div>
            </div>
          )}
        </div>
      ) : (
        <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-6 mt-8">
          {apps.map((app) => (
            <LazyLoad className="h-36" key={app.id}>
              <AppSummaryWidget app={app} buildMode={buildMode} />
            </LazyLoad>
          ))}
          <LazyLoad className="h-36">
            <NewAppWidget />
          </LazyLoad>
        </div>
      )}

      <OwnershipTransferRequestsModal open={showTransferModal} onClose={() => setShowTransferModal(false)} />
    </Page>
  );
}
