import { useApps } from "@features/apps";
import { SyncSubscriptionButton } from "@features/billing/SyncSubscriptionButton";
import { isBillingEnabled } from "@features/env";
import { IconAlertTriangle } from "@tabler/icons-react";

type Props = {
  reason: string;
};

const titles: Record<string, string> = {
  B: "App is locked due to billing",
  O: "App is locked due to overuse",
  T: "Expired Trial",
};

const desriptions: Record<string, string> = {
  B: "Upgrade to a suitable plan to unlock",
  O: "Upgrade to a suitable plan to unlock",
  T: "Subscribe to unlock",
};

// Locks that a subscription change can lift. A customer who just upgraded in Lemon Squeezy should not
// have to wait for the hourly re-check to see their app again.
const syncableReasons = ["O", "T"];

export function AppLockedContent(props: Props) {
  const { refetchApps } = useApps();
  const title = titles[props.reason] || "App is locked";
  const description = desriptions[props.reason] || "Contact support to unlock";
  const canSync = isBillingEnabled && syncableReasons.includes(props.reason);

  return (
    <>
      <IconAlertTriangle className="text-warning h-5 w-5" />
      <div>
        <p className="text-center">{title}</p>
        <p className="text-center text-muted-foreground">{description}</p>
      </div>
      {canSync && (
        <div className="flex flex-col items-center gap-1">
          <SyncSubscriptionButton variant="outline" size="sm" onSynced={refetchApps} />
          <p className="text-xs text-muted-foreground text-center px-4">Already changed your plan? Sync to apply it now.</p>
        </div>
      )}
    </>
  );
}
