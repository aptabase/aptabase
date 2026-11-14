import { ErrorState } from "@components/ErrorState";
import { LoadingState } from "@components/LoadingState";
import { formatRecent, formatUtcDate } from "@features/oauth/scopes";
import { Application } from "./apps";
import { useAppApiUsage } from "./query-api";

type Props = {
  app: Application;
};

export function AppApiUsage(props: Props) {
  const { isLoading, isError, data, refetch } = useAppApiUsage(props.app.id);

  if (isLoading) return <LoadingState />;
  if (isError) return <ErrorState refetch={refetch} />;
  if (!data) return null;

  return (
    <div className="space-y-4 max-w-3xl">
      <p className="text-sm text-muted-foreground">
        {data.allowance !== null ? (
          <>
            {(data.remaining ?? 0).toLocaleString()} of {data.allowance.toLocaleString()} requests left this month for
            the owner's account. Resets on {formatUtcDate(data.resetsAt)}.
          </>
        ) : (
          "No monthly limit."
        )}
      </p>

      <div>
        <h3 className="text-lg font-medium mb-2">This month</h3>
        {data.clients.length === 0 ? (
          <p className="text-sm text-muted-foreground">No API requests this month.</p>
        ) : (
          <div className="overflow-x-auto border rounded-md">
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b text-left">
                  <th scope="col" className="px-3 py-2 font-semibold">
                    Client
                  </th>
                  <th scope="col" className="px-3 py-2 font-semibold">
                    Used by
                  </th>
                  <th scope="col" className="px-3 py-2 font-semibold text-right">
                    Requests
                  </th>
                  <th scope="col" className="px-3 py-2 font-semibold text-right">
                    Refused
                  </th>
                  <th scope="col" className="px-3 py-2 font-semibold">
                    Last used
                  </th>
                </tr>
              </thead>
              <tbody>
                {data.clients.map((client) => (
                  <tr key={client.grantId} className="border-b last:border-b-0">
                    <td className="px-3 py-2">
                      {client.clientName}
                      {client.revoked && <span className="ml-2 text-xs text-muted-foreground">Revoked</span>}
                    </td>
                    <td className="px-3 py-2">{client.person}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{client.requests.toLocaleString()}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{client.rejected.toLocaleString()}</td>
                    <td className="px-3 py-2 text-muted-foreground">
                      {client.lastUsedAt ? formatRecent(client.lastUsedAt) : "Never"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
