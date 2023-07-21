import { Button, api, formatDate } from "@app/primitives";
import { useState } from "react";
import { SubscriptionStatusBadge } from "./SubscriptionStatusBadge";
import { BillingInfo } from "./useBilling";

type Props = {
  billing: BillingInfo;
};

export function CurrentPlan(props: Props) {
  const [loading, setLoading] = useState(false);

  const status = props.billing.subscription?.status;
  const expiresOn =
    status === "cancelled" ? props.billing.subscription?.endsAt : undefined;
  const isExpired = status === "expired";
  const hasSubscription = !!props.billing.subscription;

  const action = async () => {
    if (hasSubscription) {
      alert(
        `To upgrade or cancel your subscription, please contact support.
We're working on making this easier.`
      );
      return;
    }

    setLoading(true);
    const response = await api.post<{ url: string }>(`/_billing/checkout`);
    location.href = response.url;
  };

  return (
    <div className="flex flex-col space-y-1">
      <p className="flex items-center mb-1 justify-between">
        <span>{props.billing.plan.name} Plan</span>
        <span>
          ${props.billing.plan.monthlyPrice}{" "}
          <span className="text-sm text-muted-foreground">/mo + Tax</span>
        </span>
      </p>
      <p className="flex items-center mb-1 justify-between ">
        <span className="text-sm">
          {props.billing.plan.monthlyEvents.toLocaleString()}{" "}
          <span className="text-muted-foreground">events / mo</span>
        </span>
        {/* <Button variant="ghost" size="xs" onClick={action} loading={loading}>
          {hasSubscription ? "Manage" : "Upgrade"}
        </Button> */}
      </p>
      {status && !isExpired && (
        <p className="flex items-center mb-1 justify-between text-xs">
          <SubscriptionStatusBadge status={status} />
          {expiresOn && (
            <span className="text-muted-foreground">
              Expires on {formatDate(expiresOn)}
            </span>
          )}
        </p>
      )}
    </div>
  );
}
