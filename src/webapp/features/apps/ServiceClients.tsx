import { Button } from "@components/Button";
import { ErrorState } from "@components/ErrorState";
import { LoadingState } from "@components/LoadingState";
import { TextInput } from "@components/TextInput";
import { ScopeCheckboxes, allScopes, formatLastUsed, scopeLabel } from "@features/oauth/scopes";
import { formatDate } from "@fns/format-date";
import { errorMessage, request } from "@fns/query-api";
import { IconAlertTriangle, IconCopy } from "@tabler/icons-react";
import { useState } from "react";
import { toast } from "sonner";
import { twMerge } from "tailwind-merge";
import { Application } from "./apps";
import { ServiceClient, useServiceClients } from "./query-api";

type Props = {
  app: Application;
};

type IssuedSecret = {
  clientId: string;
  clientSecret: string;
};

function revokedText(reason: string | null): string {
  switch (reason) {
    case "owner":
      return "Revoked by you";
    case "app_transferred":
      return "Revoked when the app was transferred";
    default:
      return "Revoked";
  }
}

export function ServiceClients(props: Props) {
  const { isLoading, isError, data, refetch } = useServiceClients(props.app.id, props.app.hasOwnership);

  const [name, setName] = useState("");
  const [scopes, setScopes] = useState<string[]>(["stats:read"]);
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState("");
  const [issued, setIssued] = useState<IssuedSecret | undefined>();

  if (isLoading) return <LoadingState />;
  if (isError) return <ErrorState refetch={refetch} />;
  if (!data) return null;

  const liveCount = data.clients.filter((c) => !c.revokedAt).length;
  const atLimit = data.limit !== null && liveCount >= data.limit;

  const handleCreate = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setCreating(true);
    setError("");

    try {
      const created = await request<IssuedSecret>("POST", `/_apps/${props.app.id}/clients`, {
        name: name.trim(),
        scopes,
      });
      setIssued(created);
      setName("");
      setScopes(["stats:read"]);
      await refetch();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setCreating(false);
    }
  };

  return (
    <div className="space-y-8 max-w-[40rem]">
      <p className="text-sm text-muted-foreground">
        For scripts, CI and dashboards that read this app's data without a person signing in. Each client gets an ID
        and a secret and uses the OAuth client credentials grant.
      </p>

      {issued && <IssuedSecretBox issued={issued} onDismiss={() => setIssued(undefined)} />}

      <form onSubmit={handleCreate} className="space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-medium">New service client</h3>
          {data.limit !== null && (
            <span className="text-sm text-muted-foreground">
              {liveCount} of {data.limit} used
            </span>
          )}
        </div>
        <TextInput
          label="Name"
          name="name"
          required={true}
          value={name}
          minLength={2}
          maxLength={80}
          placeholder="Nightly report"
          disabled={atLimit}
          onChange={(e) => setName(e.target.value)}
          description="A name to recognise this client by, such as the script or system that uses it."
        />
        <ScopeCheckboxes scopes={allScopes} selected={scopes} disabled={atLimit} onChange={setScopes} />
        {error && (
          <p role="alert" className="text-sm text-destructive">
            {error}
          </p>
        )}
        {atLimit && !error && (
          <p className="text-sm text-muted-foreground">
            This app has reached its limit of service clients. Revoke one to create another.
          </p>
        )}
        <div>
          <Button disabled={atLimit || creating || name.trim().length < 2 || scopes.length === 0} loading={creating}>
            Create
          </Button>
        </div>
      </form>

      {data.clients.length > 0 && (
        <ul className="space-y-2">
          {data.clients.map((client) => (
            <ServiceClientItem
              key={client.clientId}
              app={props.app}
              client={client}
              onRotated={setIssued}
              onChanged={refetch}
            />
          ))}
        </ul>
      )}
    </div>
  );
}

function CopyRow(props: { label: string; value: string }) {
  const copy = async () => {
    try {
      await navigator.clipboard.writeText(props.value);
      toast(`${props.label} copied.`);
    } catch {
      toast.error("Could not copy. Select the text and copy it manually.");
    }
  };

  return (
    <div>
      <p className="text-xs text-muted-foreground">{props.label}</p>
      <div className="flex items-center gap-2">
        <code className="flex-1 min-w-0 break-all font-mono text-sm bg-background border rounded px-2 py-1">
          {props.value}
        </code>
        <Button type="button" variant="outline" size="sm" onClick={copy} aria-label={`Copy ${props.label.toLowerCase()}`}>
          <IconCopy className="w-4 h-4 mr-1" />
          Copy
        </Button>
      </div>
    </div>
  );
}

function IssuedSecretBox(props: { issued: IssuedSecret; onDismiss: () => void }) {
  return (
    <div role="status" className="border border-warning bg-warning/10 rounded-md p-4 space-y-3">
      <p className="text-sm font-medium">Copy the secret now. It is not stored and cannot be shown again.</p>
      <CopyRow label="Client ID" value={props.issued.clientId} />
      <CopyRow label="Client secret" value={props.issued.clientSecret} />
      <Button type="button" variant="outline" size="sm" onClick={props.onDismiss}>
        Done
      </Button>
    </div>
  );
}

function ServiceClientItem(props: {
  app: Application;
  client: ServiceClient;
  onRotated: (issued: IssuedSecret) => void;
  onChanged: () => unknown;
}) {
  const { client } = props;
  const [busy, setBusy] = useState(false);
  const revoked = !!client.revokedAt;
  const path = `/_apps/${props.app.id}/clients/${encodeURIComponent(client.clientId)}`;

  const run = async (action: () => Promise<void>) => {
    setBusy(true);
    try {
      await action();
      await props.onChanged();
    } catch (e) {
      toast.error(errorMessage(e));
    } finally {
      setBusy(false);
    }
  };

  const rotate = () => {
    if (!window.confirm(`Rotate the secret of '${client.name}'? The current secret stops working immediately.`)) return;

    run(async () => {
      props.onRotated(await request<IssuedSecret>("POST", `${path}/rotate`));
    });
  };

  const revoke = () => {
    if (!window.confirm(`Revoke service client '${client.name}'? This cannot be undone.`)) return;

    run(async () => {
      await request("DELETE", path);
      toast(`${client.name} was revoked.`);
    });
  };

  return (
    <li className={twMerge("border rounded-md px-4 py-3 bg-card", revoked && "bg-muted text-muted-foreground")}>
      <div className="flex items-start justify-between gap-4">
        <div className="min-w-0">
          <p className="font-medium break-words">{client.name}</p>
          <p className="font-mono text-xs break-all">{client.clientId}</p>
        </div>
        {!revoked && (
          <div className="flex gap-2 shrink-0">
            <Button type="button" variant="outline" size="sm" disabled={busy} onClick={rotate}>
              Rotate secret
            </Button>
            <Button type="button" variant="destructive" size="sm" disabled={busy} onClick={revoke}>
              Revoke
            </Button>
          </div>
        )}
      </div>
      <p className="mt-2 text-sm">
        <span className="text-muted-foreground">Secret </span>
        <span className="font-mono">{client.secretPrefix}…</span>
        <span className="text-muted-foreground"> · {client.scopes.map(scopeLabel).join(", ")}</span>
      </p>
      <p className="mt-1 text-xs text-muted-foreground">
        Created {formatDate(client.createdAt)} · {formatLastUsed(client.lastUsedAt)}
      </p>
      {!revoked && client.deniedCount > 0 && (
        <p className="mt-2 text-xs text-warning flex items-center gap-1">
          <IconAlertTriangle className="w-4 h-4 shrink-0" />
          {client.deniedCount} recent {client.deniedCount === 1 ? "request was" : "requests were"} refused because{" "}
          {client.deniedCount === 1 ? "it" : "they"} asked for another app
        </p>
      )}
      {revoked && <p className="mt-2 text-sm">{revokedText(client.revokedReason)}</p>}
    </li>
  );
}
