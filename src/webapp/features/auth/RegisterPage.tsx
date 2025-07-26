import { Button } from "@components/Button";
import { Page } from "@components/Page";
import { TextInput } from "@components/TextInput";
import { DEFAULT_OAUTH_STATUS, requestRegisterLink } from "@features/auth";
import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { DataResidency } from "./DataResidency";
import { LegalNotice } from "./LegalNotice";
import { Logo } from "./Logo";
import { OAuthButtons } from "./OAuthButtons";
import { RegionSwitch } from "./RegionSwitch";

type FormStatus = "idle" | "loading" | "success";

type OAuthStatus = {
  github: boolean;
  google: boolean;
  authentik: boolean;
  emailAuthDisabled: boolean;
};

type StatusMessageProps = {
  status: FormStatus;
};

const StatusMessage = (props: StatusMessageProps) => {
  if (props.status === "success") {
    return <span className="text-success">Woo-hoo! Email sent, go check your inbox!</span>;
  }

  return (
    <>
      Already registered?{" "}
      <Link className="font-medium text-foreground" to="/auth">
        Sign in
      </Link>{" "}
      to your account.
    </>
  );
};

Component.displayName = "RegisterPage";
export function Component() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [status, setStatus] = useState<FormStatus>("idle");
  const [oauthStatus, setOauthStatus] = useState<OAuthStatus | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const checkOAuthStatus = async () => {
      try {
        const response = await fetch("/api/_auth/oauth-status");
        if (response.ok) {
          const status = await response.json();
          setOauthStatus(status);
        } else {
          setOauthStatus(DEFAULT_OAUTH_STATUS);
        }
      } catch {
        setOauthStatus(DEFAULT_OAUTH_STATUS);
      } finally {
        setLoading(false);
      }
    };

    checkOAuthStatus();
  }, []);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setStatus("loading");

    await requestRegisterLink(name, email);

    setStatus("success");
  };

  // Show loading state while checking OAuth status
  if (loading) {
    return (
      <Page title="Sign up">
        <div className="sm:mx-auto sm:w-full sm:max-w-md">
          <Logo className="mx-auto h-12 w-auto text-primary" />
          <h2 className="text-center text-3xl font-bold">Sign up for an account</h2>
          <DataResidency />
        </div>
        <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
          <div className="py-8 px-4 sm:rounded-lg sm:px-10">
            <div className="text-center text-muted-foreground">Loading...</div>
          </div>
        </div>
      </Page>
    );
  }

  // If email auth is disabled and no OAuth providers are available, show a message
  if (oauthStatus?.emailAuthDisabled && !oauthStatus?.github && !oauthStatus?.google && !oauthStatus?.authentik) {
    return (
      <Page title="Sign up">
        <div className="sm:mx-auto sm:w-full sm:max-w-md">
          <Logo className="mx-auto h-12 w-auto text-primary" />
          <h2 className="text-center text-3xl font-bold">Sign up for an account</h2>
          <DataResidency />
        </div>
        <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
          <div className="py-8 px-4 sm:rounded-lg sm:px-10">
            <div className="text-center text-muted-foreground">
              <p>No authentication methods are currently available.</p>
              <p className="mt-2 text-sm">Please contact your administrator to configure authentication.</p>
            </div>
          </div>
        </div>
      </Page>
    );
  }

  return (
    <Page title="Sign up">
      <div className="sm:mx-auto sm:w-full sm:max-w-md">
        <Logo className="mx-auto h-12 w-auto text-primary" />
        <h2 className="text-center text-3xl font-bold">Sign up for an account</h2>
        <DataResidency />
      </div>
      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="py-8 px-4 sm:rounded-lg sm:px-10">
          <OAuthButtons />

          {!oauthStatus?.emailAuthDisabled && (
            <form onSubmit={handleSubmit} className="flex flex-col space-y-4">
              <TextInput
                label="Name"
                name="name"
                placeholder="Peter Parker"
                value={name}
                required={true}
                onChange={(e) => setName(e.target.value)}
              />
              <TextInput
                label="Email Address"
                name="email"
                type="email"
                placeholder="peter.parker@corp.com"
                autoComplete="email"
                value={email}
                required={true}
                onChange={(e) => setEmail(e.target.value)}
              />
              <Button loading={status === "loading"}>Send magic link</Button>
              <p className="text-center text-sm h-10 text-muted-foreground">
                <StatusMessage status={status} />
              </p>
            </form>
          )}
        </div>
        <LegalNotice operation="signup" />
        <RegionSwitch />
      </div>
    </Page>
  );
}
