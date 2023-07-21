/*
Possible statuses:
- active
- cancelled
- expired
- paused
- past_due
- unpaid
*/

const statuses: Record<string, [string, string]> = {
  active: ["Active", "text-success"],
  cancelled: ["Cancelled", "text-destructive"],
  expired: ["Expired", "text-destructive"],
  paused: ["Paused", "text-primary"],
  past_due: ["Past Due", "text-destructive"],
  unpaid: ["Unpaid", "text-destructive"],
};

export function SubscriptionStatusBadge(props: { status: string }) {
  const [statusFormatted, color] = statuses[props.status] ?? [props.status, ""];
  return <span className={color}>{statusFormatted}</span>;
}
