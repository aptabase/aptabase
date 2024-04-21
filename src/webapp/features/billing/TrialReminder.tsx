import { Link } from "react-router-dom";
import { useBilling } from "./useBilling";
import { Alert, AlertDescription } from "@components/Alert";
import { isBillingEnabled } from "@features/env";

export function TrialReminder() {
  const { data: billing } = useBilling();

  if (!isBillingEnabled || !billing || !billing.plan.freeTrialEndsAt) return null;

  const endsAt = new Date(billing.plan.freeTrialEndsAt);
  const differenceInMs = new Date(endsAt).getTime() - new Date().getTime();
  const days = Math.floor(differenceInMs / (1000 * 60 * 60 * 24));

  if (days <= -1) return null;

  return (
    <Link to="/billing">
      <Alert variant="warning" className="p-2">
        <AlertDescription className="text-sm">
          Trial{" "}
          {days === 1 ? `ends tomorrow` : days > 0 ? `ends in ${days} days` : days === 0 ? "ends today" : "expired"}
        </AlertDescription>
      </Alert>
    </Link>
  );
}
