import { Badge } from "@components/Badge";
import { formatDate } from "@fns/format-date";
import { formatDistanceToNow } from "date-fns";

export const allScopes = ["stats:read", "sessions:read"];

const scopeInfo: Record<string, { label: string; helper: string }> = {
  "stats:read": {
    label: "Read aggregated statistics",
    helper: "Metrics, charts and breakdowns, the list of your apps and their usage.",
  },
  "sessions:read": {
    label: "Read individual sessions and events",
    helper: "Session timelines and event properties, which may contain personal data your app sent.",
  },
};

export function scopeLabel(scope: string): string {
  return scopeInfo[scope]?.label ?? scope;
}

export const checkboxClassName = "mt-0.5 h-4 w-4 shrink-0 accent-primary";

type ScopeCheckboxesProps = {
  scopes: string[];
  selected: string[];
  disabled?: boolean;
  onChange: (selected: string[]) => void;
};

export function ScopeCheckboxes(props: ScopeCheckboxesProps) {
  const toggle = (scope: string, checked: boolean) => {
    // Keeps the order of the offered scopes regardless of click order
    props.onChange(props.scopes.filter((s) => (s === scope ? checked : props.selected.includes(s))));
  };

  return (
    <div className="space-y-3">
      {props.scopes.map((scope) => (
        <label key={scope} className="flex items-start gap-2 text-sm">
          <input
            type="checkbox"
            className={checkboxClassName}
            checked={props.selected.includes(scope)}
            disabled={props.disabled}
            onChange={(e) => toggle(scope, e.target.checked)}
          />
          <span>
            <span className="block font-medium">{scopeLabel(scope)}</span>
            {scopeInfo[scope] && <span className="block text-muted-foreground">{scopeInfo[scope].helper}</span>}
          </span>
        </label>
      ))}
    </div>
  );
}

export function VerifiedBadge(props: { verified: boolean }) {
  return props.verified ? (
    <Badge variant="outline" className="border-success text-success">
      Verified
    </Badge>
  ) : (
    <Badge variant="outline" className="border-warning text-warning">
      Unverified
    </Badge>
  );
}

// Relative for the last 30 days, a date before that
export function formatRecent(value: string): string {
  const date = new Date(value);
  const thirtyDays = 30 * 24 * 60 * 60 * 1000;
  if (Date.now() - date.getTime() < thirtyDays) {
    return formatDistanceToNow(date, { addSuffix: true });
  }

  return formatDate(date);
}

export function formatLastUsed(value: string | null | undefined): string {
  return value ? `Last used ${formatRecent(value)}` : "Never used";
}

export function formatUtcDate(value: string): string {
  return new Date(value).toLocaleDateString("en-US", {
    timeZone: "UTC",
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}
