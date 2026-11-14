import { formatUtcDate } from "@features/oauth/scopes";
import { getOptional } from "@fns/query-api";
import { useQuery } from "@tanstack/react-query";
import { twMerge } from "tailwind-merge";

type BillingApiUsage = {
  tier: string;
  allowance: number | null;
  used: number;
  remaining: number | null;
  resetsAt: string;
  maxWindowDays: number | null;
  apps: { appId: string; appName: string; requests: number; rejected: number }[];
};

export function ApiUsageCard() {
  // null when the query API is switched off on the server
  const { data } = useQuery({
    queryKey: ["billing-api-usage"],
    queryFn: () => getOptional<BillingApiUsage>("/_billing/api-usage"),
    retry: false,
    refetchOnMount: true,
  });

  if (!data) return null;

  const percentage = data.allowance ? Math.min((data.used / data.allowance) * 100, 100) : 0;
  const exhausted = data.allowance !== null && data.used >= data.allowance;

  return (
    <div className="border rounded-md p-4">
      <div className="flex items-center mb-1 justify-between">
        <span>API usage</span>
        <span className="text-sm text-muted-foreground">Resets on {formatUtcDate(data.resetsAt)} (UTC)</span>
      </div>

      {data.allowance !== null ? (
        <div className="space-y-1">
          <div className="text-sm">
            {data.used.toLocaleString()} of {data.allowance.toLocaleString()} API requests used this month
          </div>
          <div className="overflow-hidden rounded bg-accent">
            <div
              className={twMerge("h-2 rounded", exhausted ? "bg-destructive" : "bg-primary")}
              style={{ width: `${percentage}%` }}
            />
          </div>
        </div>
      ) : (
        <div className="text-sm">{data.used.toLocaleString()} API requests this month, no limit</div>
      )}

      {data.maxWindowDays !== null && (
        <p className="mt-2 text-sm text-muted-foreground">Queries can span up to {data.maxWindowDays} days.</p>
      )}

      {data.apps.length > 0 && (
        <table className="mt-4 w-full text-sm">
          <thead>
            <tr className="border-b text-left text-muted-foreground">
              <th scope="col" className="py-1 font-normal">
                App
              </th>
              <th scope="col" className="py-1 font-normal text-right">
                Requests
              </th>
              <th scope="col" className="py-1 pl-4 font-normal text-right">
                Refused
              </th>
            </tr>
          </thead>
          <tbody>
            {data.apps.map((app) => (
              <tr key={app.appId} className="border-b last:border-b-0">
                <td className="py-1">{app.appName}</td>
                <td className="py-1 text-right tabular-nums">{app.requests.toLocaleString()}</td>
                <td className="py-1 pl-4 text-right tabular-nums">{app.rejected.toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
