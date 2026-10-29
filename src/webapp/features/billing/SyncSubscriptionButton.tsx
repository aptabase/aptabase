import { Button, ButtonProps } from "@components/Button";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@components/Tooltip";
import { api } from "@fns/api";
import { IconRefresh } from "@tabler/icons-react";
import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { toast } from "sonner";

type SyncResult = {
  outcome: "no_subscription" | "unavailable" | "unchanged" | "updated";
  unlocked: boolean;
  plan?: { name: string; monthlyEvents: number };
  // lock still in place after the sync: "O" overuse, "T" expired trial, null when none
  lockReason?: string | null;
};

type Props = {
  size?: ButtonProps["size"];
  variant?: ButtonProps["variant"];
  className?: string;
  // Called after a successful sync, e.g. to refetch the apps list so a lifted lock shows right away
  onSynced?: (result: SyncResult) => Promise<void> | void;
};

// Re-reads the subscription from Lemon Squeezy on demand. It is the manual counterpart of the check the
// overuse job runs before locking, for the customer who just changed plan and does not want to wait.
export function SyncSubscriptionButton(props: Props) {
  const [loading, setLoading] = useState(false);
  const queryClient = useQueryClient();

  const sync = async () => {
    setLoading(true);
    try {
      const result = await api.post<SyncResult>("/_billing/sync");
      await queryClient.invalidateQueries({ queryKey: ["billing"] });
      await props.onSynced?.(result);
      notify(result);
    } catch {
      toast.error("Could not sync your subscription. Please try again in a moment.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <TooltipProvider>
      <Tooltip delayDuration={200}>
        <TooltipTrigger asChild>
          <Button
            variant={props.variant ?? "ghost"}
            size={props.size ?? "xs"}
            className={props.className}
            onClick={sync}
            loading={loading}
          >
            <IconRefresh className="h-4 w-4 mr-1" />
            Sync subscription
          </Button>
        </TooltipTrigger>
        <TooltipContent className="max-w-xs">
          <p>
            Re-checks your subscription with Lemon Squeezy and applies any plan change that is not reflected here
            yet. If your account is paused and the current plan has room, it is unpaused.
          </p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}

function notify(result: SyncResult) {
  const planLine =
    result.outcome === "updated" ? `Your plan is now ${result.plan?.name}.` : `Your plan is up to date: ${result.plan?.name}.`;

  switch (result.outcome) {
    case "no_subscription":
      toast("You don't have a subscription to sync. Use Upgrade to subscribe.");
      return;
    case "unavailable":
      toast.error("Lemon Squeezy did not answer. Your stored plan was kept, please try again in a minute.");
      return;
    case "updated":
    case "unchanged":
      if (result.unlocked) {
        toast.success(`${planLine} Your account is unpaused and events will be accepted again shortly.`);
      } else if (result.lockReason === "O") {
        toast(`${planLine} This month's usage is still over its limit, so your account stays paused until you upgrade.`);
      } else if (result.lockReason) {
        toast(`${planLine} Your account is still locked; contact support if this looks wrong.`);
      } else {
        toast.success(planLine);
      }
      return;
  }
}
