import { api } from "@fns/api";
import { useState } from "react";
import { SubscriptionStatusBadge } from "./SubscriptionStatusBadge";
import { BillingInfo } from "./useBilling";
import { Button } from "@components/Button";
import { formatDate } from "@fns/format-date";

type Props = {
  billing: BillingInfo;
};

export function CurrentPlan(props: Props) {
  const [loading, setLoading] = useState(false);

  const status = props.billing.subscription?.status;
  const expiresOn = status === "cancelled" ? props.billing.subscription?.endsAt : undefined;
  const isExpired = status === "expired";
  const hasSubscription = !!props.billing.subscription;

  const action = async () => {
    setLoading(true);

    const path = hasSubscription ? `/_billing/portal` : `/_billing/checkout`;
    const response = await api.post<{ url: string }>(path);
    location.href = response.url;
  };

  return (
    <div className="flex flex-col space-y-1">
      <p className="flex items-center mb-1 justify-between">
        <span>{props.billing.plan.name}</span>
        {props.billing.plan.monthlyPrice > 0 && (
          <span>
            ${props.billing.plan.monthlyPrice} <span className="text-sm text-muted-foreground">/mo + Tax</span>
          </span>
        )}
      </p>
      <p className="flex items-center mb-1 justify-between -mr-2">
        {props.billing.plan.freeTrialEndsAt ? (
          <span className="text-sm space-x-1">
            <span className="text-muted-foreground">Free access until</span>
            <span>{new Date(props.billing.plan.freeTrialEndsAt).toLocaleDateString()}</span>
          </span>
        ) : (
          <span className="text-sm">
            {props.billing.plan.monthlyEvents.toLocaleString()}{" "}
            <span className="text-muted-foreground">events / mo</span>
          </span>
        )}
        <Button variant="secondary" size="xs" onClick={action} loading={loading}>
          {hasSubscription ? "Manage Subscription" : "Upgrade"}
        </Button>
      </p>
      {status && !isExpired && (
        <p className="flex items-center mb-1 justify-between text-xs">
          <SubscriptionStatusBadge status={status} />
          {expiresOn && <span className="text-muted-foreground">Expires on {formatDate(expiresOn)}</span>}
        </p>
      )}
    </div>
  );
}
