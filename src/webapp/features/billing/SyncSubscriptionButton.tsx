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
  const unlockedNote = result.unlocked ? " Your account is unpaused and events will be accepted again shortly." : "";

  switch (result.outcome) {
    case "no_subscription":
      toast("You don't have a subscription to sync. Use Upgrade to subscribe.");
      return;
    case "unavailable":
      toast.error("Lemon Squeezy did not answer. Your stored plan was kept, please try again in a minute.");
      return;
    case "updated":
      toast.success(`Your plan is now ${result.plan?.name}.${unlockedNote}`);
      return;
    case "unchanged":
      toast.success(`Your plan is up to date: ${result.plan?.name}.${unlockedNote}`);
      return;
  }
}
