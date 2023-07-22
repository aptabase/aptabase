import { requestSignInLink } from "@app/auth";
import { Button, Logo, Page, TextInput } from "@app/primitives";
import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { DataResidency } from "./components/DataResidency";
import { LegalNotice } from "./components/LegalNotice";
import { RegionSwitch } from "./components/RegionSwitch";
import { SignInWithGitHub } from "./components/SignInWithGitHub";
import { SignInWithGoogle } from "./components/SignInWithGoogle";
import { isOAuthEnabled } from "@app/env";

type FormStatus = "idle" | "loading" | "success" | "notfound";

type StatusMessageProps = {
  status: FormStatus;
};

const SignUpMessage = () => (
  <span className="block">
    Don't have an account?{" "}
    <Link className="font-semibold text-foreground" to="/auth/register">
      Sign up
    </Link>{" "}
    for free.
  </span>
);

const StatusMessage = (props: StatusMessageProps) => {
  if (props.status === "success") {
    return (
      <span className="text-success">
        Woo-hoo! Email sent, go check your inbox!
      </span>
    );
  }

  if (props.status === "notfound") {
    return (
      <>
        <span className="text-destructive">
          Could not find an account with that email.
        </span>
        <SignUpMessage />
      </>
    );
  }

  return <SignUpMessage />;
};

const RedirectErrorMessage = () => {
  const [params] = useSearchParams();

  const error = params.get("error");
  if (!error) {
    return null;
  }
  const message =
    error === "expired" ? "This link has expired." : "This link is invalid.";

  return (
    <p className="mx-auto text-center mb-10 text-destructive text-sm">
      {message} Please request a new one.
    </p>
  );
};

Component.displayName = "Login";
export function Component() {
  const [email, setEmail] = useState("");
  const [status, setStatus] = useState<FormStatus>("idle");

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setStatus("loading");

    const found = await requestSignInLink(email);
    setStatus(found ? "success" : "notfound");
  };

  return (
    <Page title="Login">
      <div className="mx-auto text-center mb-10">
        <RedirectErrorMessage />
      </div>

      <div className="sm:mx-auto sm:w-full sm:max-w-md text-center">
        <Logo className="mx-auto h-20 w-auto text-primary" />
        <h2 className="text-center text-3xl text-foreground font-bold">
          Sign in to your account
        </h2>
        <DataResidency />
      </div>
      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="py-8 px-4 sm:rounded-lg sm:px-10">
          {isOAuthEnabled && (
            <>
              <div className="space-y-2">
                <SignInWithGitHub />
                <SignInWithGoogle />
              </div>

              <div className="relative my-4">
                <div
                  className="absolute inset-0 flex items-center"
                  aria-hidden="true"
                >
                  <div className="w-full border-t" />
                </div>
                <div className="relative flex justify-center text-sm">
                  <span className="px-2 bg-muted">OR</span>
                </div>
              </div>
            </>
          )}

          <form onSubmit={handleSubmit} className="flex flex-col space-y-4">
            <TextInput
              label="Enter your email address"
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
        </div>
        <LegalNotice operation="signin" />
        <RegionSwitch />
      </div>
    </Page>
  );
}
