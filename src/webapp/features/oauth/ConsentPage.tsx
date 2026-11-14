import { Button } from "@components/Button";
import { LoadingState } from "@components/LoadingState";
import { Page } from "@components/Page";
import { AppIcon } from "@features/apps/AppIcon";
import { me } from "@features/auth/auth";
import { Logo } from "@features/auth/Logo";
import { api } from "@fns/api";
import { RequestError, errorMessage, request } from "@fns/query-api";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { ScopeCheckboxes, VerifiedBadge, checkboxClassName } from "./scopes";

type ConsentInfo = {
  // Identifies the request this page displays. Sent back with the decision so the
  // server can refuse it if another request replaced this one in the meantime.
  requestId: string;
  client: { name: string; url: string; host: string; verified: boolean };
  scopes: string[];
  requestedScopes: string[];
  apps: { id: string; name: string; iconPath: string; hasOwnership: boolean }[];
  current: null | { scopes: string[]; allApps: boolean; appIds: string[] };
};

type ConsentState =
  | { kind: "ready"; info: ConsentInfo }
  | { kind: "signin" }
  | { kind: "error"; message: string };

const signInPath = "/auth?consent=1";
const genericError = "This authorization request could not be loaded. Start again from the application you want to connect.";

async function loadConsent(): Promise<ConsentState> {
  const [status, response] = await api.fetch("GET", "/_oauth/consent");
  if (status === 200) return { kind: "ready", info: (await response.json()) as ConsentInfo };
  if (status === 401) return { kind: "signin" };

  try {
    const body = (await response.json()) as { message?: string };
    return { kind: "error", message: body.message || genericError };
  } catch {
    return { kind: "error", message: genericError };
  }
}

function Heading(props: { children: React.ReactNode }) {
  return (
    <div className="sm:mx-auto sm:w-full sm:max-w-md text-center">
      <Logo className="mx-auto h-12 w-auto text-primary" />
      <h2 className="text-center text-3xl text-foreground font-bold">{props.children}</h2>
    </div>
  );
}

Component.displayName = "ConsentPage";
export function Component() {
  const { isLoading, data } = useQuery({
    queryKey: ["oauth-consent"],
    queryFn: loadConsent,
    retry: false,
    staleTime: Infinity,
    gcTime: 0,
  });

  if (isLoading) return <LoadingState size="lg" color="primary" />;
  if (data?.kind === "signin") return <Navigate to={signInPath} replace={true} />;

  return (
    <Page title="Connect an application">
      {data?.kind === "ready" ? (
        <ConsentForm info={data.info} />
      ) : (
        <>
          <Heading>Cannot connect the application</Heading>
          <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
            <p role="alert" className="text-center text-sm text-destructive">
              {data?.kind === "error" ? data.message : genericError}
            </p>
            <p className="mt-4 text-center text-sm text-muted-foreground">Nothing was shared. You can close this page.</p>
          </div>
        </>
      )}
    </Page>
  );
}

type AppsMode = "selected" | "all";

function ConsentForm(props: { info: ConsentInfo }) {
  const { client, scopes, apps, current } = props.info;
  const navigate = useNavigate();
  const { data: account } = useQuery({ queryKey: ["me"], queryFn: me, retry: false });

  const hasApps = apps.length > 0;
  const [selectedScopes, setSelectedScopes] = useState<string[]>(scopes);
  const [mode, setMode] = useState<AppsMode>(current?.allApps ? "all" : "selected");
  const [appIds, setAppIds] = useState<string[]>(
    current && !current.allApps ? apps.filter((a) => current.appIds.includes(a.id)).map((a) => a.id) : []
  );
  const [busy, setBusy] = useState<"allow" | "deny" | undefined>();
  const [error, setError] = useState("");

  const canAllow = selectedScopes.length > 0 && (mode === "all" || appIds.length > 0);

  const toggleApp = (id: string, checked: boolean) => {
    setAppIds(apps.map((a) => a.id).filter((x) => (x === id ? checked : appIds.includes(x))));
  };

  const decide = async (allow: boolean) => {
    setBusy(allow ? "allow" : "deny");
    setError("");

    const requestId = props.info.requestId;
    const body = allow
      ? { requestId, allow: true, scopes: selectedScopes, appIds: mode === "all" ? null : appIds }
      : { requestId, allow: false, scopes: [], appIds: null };

    try {
      const { redirectUrl } = await request<{ redirectUrl: string }>("POST", "/_oauth/consent", body);
      window.location.assign(redirectUrl);
    } catch (e) {
      if (e instanceof RequestError && e.status === 401) return navigate(signInPath, { replace: true });

      setError(errorMessage(e));
      setBusy(undefined);
    }
  };

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (canAllow && !busy) decide(true);
  };

  return (
    <>
      <Heading>Connect an application</Heading>
      <form onSubmit={handleSubmit} className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-background border rounded-lg p-6 space-y-6">
          <div>
            <div className="flex items-center justify-between gap-2">
              <p className="font-medium break-words min-w-0">{client.name}</p>
              <VerifiedBadge verified={client.verified} />
            </div>
            <p className="text-sm text-muted-foreground break-all">{client.url}</p>
            {!client.verified && (
              <p className="mt-2 text-sm text-warning">
                Aptabase has not verified who runs this application. Only continue if you started this from an
                application you trust.
              </p>
            )}
            <p className="mt-3 text-sm">This application is asking for access to your Aptabase data.</p>
          </div>

          <fieldset>
            <legend className="text-sm font-medium mb-3">Permissions</legend>
            <ScopeCheckboxes scopes={scopes} selected={selectedScopes} onChange={setSelectedScopes} />
            {selectedScopes.length === 0 && (
              <p className="mt-2 text-sm text-muted-foreground">Keep at least one permission to allow access.</p>
            )}
          </fieldset>

          <fieldset>
            <legend className="text-sm font-medium mb-3">Apps</legend>
            {!hasApps && <p className="mb-3 text-sm text-muted-foreground">You have no apps yet.</p>}
            <div className="space-y-3">
              {hasApps && (
                <div>
                  <label className="flex items-start gap-2 text-sm">
                    <input
                      type="radio"
                      name="apps-mode"
                      className={checkboxClassName}
                      checked={mode === "selected"}
                      onChange={() => setMode("selected")}
                    />
                    <span className="font-medium">Selected apps</span>
                  </label>
                  {mode === "selected" && (
                    <ul className="mt-2 ml-6 space-y-2 max-h-56 overflow-y-auto">
                      {apps.map((app) => (
                        <li key={app.id}>
                          <label className="flex items-center gap-2 text-sm">
                            <input
                              type="checkbox"
                              className="h-4 w-4 shrink-0 accent-primary"
                              checked={appIds.includes(app.id)}
                              onChange={(e) => toggleApp(app.id, e.target.checked)}
                            />
                            <AppIcon iconPath={app.iconPath} className="w-5 h-5 shrink-0" />
                            <span className="truncate">{app.name}</span>
                            {!app.hasOwnership && <span className="text-xs text-muted-foreground">Shared with you</span>}
                          </label>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              )}
              <label className="flex items-start gap-2 text-sm">
                <input
                  type="radio"
                  name="apps-mode"
                  className={checkboxClassName}
                  checked={mode === "all"}
                  onChange={() => setMode("all")}
                />
                <span>
                  <span className="block font-medium">All apps</span>
                  <span className="block text-muted-foreground">
                    Includes apps you create or that are shared with you in the future.
                  </span>
                </span>
              </label>
            </div>
          </fieldset>

          {error && (
            <p role="alert" className="text-sm text-destructive">
              {error}
            </p>
          )}

          <div className="flex gap-2">
            <Button
              type="button"
              variant="outline"
              className="flex-1"
              disabled={!!busy}
              onClick={() => decide(false)}
            >
              Deny
            </Button>
            <Button type="submit" className="flex-1" disabled={!canAllow || !!busy} loading={busy === "allow"}>
              Allow access
            </Button>
          </div>
        </div>

        {account && (
          <p className="mt-4 text-center text-sm text-muted-foreground">
            Signed in as <span className="text-foreground">{account.email}</span>
          </p>
        )}
      </form>
    </>
  );
}
