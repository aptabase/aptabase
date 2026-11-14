import { Button } from "@components/Button";
import { ErrorState } from "@components/ErrorState";
import { LoadingState } from "@components/LoadingState";
import { Page, PageHeading } from "@components/Page";
import { formatDate } from "@fns/format-date";
import { errorMessage, request } from "@fns/query-api";
import { useState } from "react";
import { toast } from "sonner";
import { twMerge } from "tailwind-merge";
import { Grant, useGrants } from "./grants";
import { VerifiedBadge, formatLastUsed, scopeLabel } from "./scopes";

function revokedText(reason: string | null): string {
  switch (reason) {
    case "user":
      return "Revoked by you";
    case "token_reuse":
      return "Revoked automatically: a refresh token was used twice, which can mean it was copied";
    case "access_failures":
      return "Revoked automatically: too many requests for apps it could not access";
    default:
      return "Revoked";
  }
}

Component.displayName = "ConnectedAppsPage";
export function Component() {
  const { isLoading, isError, data: grants, refetch } = useGrants();

  return (
    <Page title="Connected apps">
      <PageHeading title="Connected apps" subtitle="Applications that can read your Aptabase data" />

      <div className="flex flex-col gap-4 max-w-3xl mt-8">
        {isLoading && <LoadingState />}
        {isError && <ErrorState refetch={refetch} />}
        {grants === null && <p className="text-sm text-muted-foreground">Connected apps are not available on this server.</p>}
        {grants && grants.length === 0 && (
          <p className="text-sm text-muted-foreground">No applications are connected to your account.</p>
        )}
        {grants && grants.length > 0 && (
          <ul className="space-y-4">
            {grants.map((grant) => (
              <GrantItem key={grant.id} grant={grant} onRevoked={refetch} />
            ))}
          </ul>
        )}
      </div>
    </Page>
  );
}

function GrantItem(props: { grant: Grant; onRevoked: () => unknown }) {
  const { grant } = props;
  const [revoking, setRevoking] = useState(false);
  const revoked = !!grant.revokedAt;

  const revoke = async () => {
    if (!window.confirm(`Revoke access for '${grant.clientName}'? It will no longer be able to read your data.`)) return;

    setRevoking(true);
    try {
      await request("DELETE", `/_oauth/grants/${encodeURIComponent(grant.id)}`);
      toast(`Access for ${grant.clientName} was revoked.`);
      await props.onRevoked();
    } catch (e) {
      toast.error(errorMessage(e));
    } finally {
      setRevoking(false);
    }
  };

  return (
    <li className={twMerge("border rounded-md p-4 bg-card", revoked && "bg-muted text-muted-foreground")}>
      <div className="flex items-start justify-between gap-4">
        <div className="min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="font-medium break-words">{grant.clientName}</span>
            <VerifiedBadge verified={grant.verified} />
          </div>
          <p className="text-sm text-muted-foreground break-all">{grant.clientUrl}</p>
        </div>
        {!revoked && (
          <Button variant="destructive" size="sm" className="shrink-0" disabled={revoking} onClick={revoke}>
            Revoke
          </Button>
        )}
      </div>

      <dl className="mt-3 text-sm space-y-1">
        <div className="flex gap-2">
          <dt className="text-muted-foreground shrink-0 w-24">Permissions</dt>
          <dd>{grant.scopes.map(scopeLabel).join(", ")}</dd>
        </div>
        <div className="flex gap-2">
          <dt className="text-muted-foreground shrink-0 w-24">Apps</dt>
          <dd>{grant.allApps ? "All apps" : grant.apps.map((a) => a.name).join(", ") || "None you can access"}</dd>
        </div>
      </dl>

      <p className="mt-3 text-xs text-muted-foreground">
        Connected {formatDate(grant.consentedAt)} · {formatLastUsed(grant.lastUsedAt)}
      </p>
      {revoked && <p className="mt-2 text-sm">{revokedText(grant.revokedReason)}</p>}
    </li>
  );
}
