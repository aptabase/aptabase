import { Button } from "@components/Button";
import { Page } from "@components/Page";
import { TextInput } from "@components/TextInput";
import { requestSignInLink } from "@features/auth";
import { isOAuthEnabled } from "@features/env";
import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { DataResidency } from "./DataResidency";
import { LegalNotice } from "./LegalNotice";
import { Logo } from "./Logo";
import { RegionSwitch } from "./RegionSwitch";
import { SignInWithGitHub } from "./SignInWithGitHub";
import { SignInWithGoogle } from "./SignInWithGoogle";

type FormStatus = "idle" | "loading" | "success" | "notfound" | "error";

type StatusMessageProps = {
  status: FormStatus;
  error?: string | null;
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
      <>
        <span className="text-success">Woo-hoo! Email sent, go check your inbox!</span>
        <br />
        <SignUpMessage />
      </>
    );
  }

  if (props.status === "notfound") {
    return (
      <>
        <span className="text-destructive">Could not find an account with that email.</span>
        <br />
        <SignUpMessage />
      </>
    );
  }

  if (props.status === "error" && props.error) {
    return (
      <>
        <span className="text-destructive">{props.error}</span>
        <br />
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
  const message = error === "expired" ? "This link has expired." : "This link is invalid.";

  return <p className="mx-auto text-center mb-10 text-destructive text-sm">{message} Please request a new one.</p>;
};

Component.displayName = "LoginPage";
export function Component() {
  const [email, setEmail] = useState("");
  const [status, setStatus] = useState<FormStatus>("idle");
  const [emailError, setEmailError] = useState<string | null>(null);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setStatus("loading");
    setEmailError(null);

    const result: any = await requestSignInLink(email);
    if (typeof result === "string") {
      setEmailError(result);
      setStatus("error");
      return;
    }
    if (result && typeof result === "object" && result.errors) {
      const firstError = Object.values(result.errors).flat()[0];
      if (firstError) {
        setEmailError(firstError as string);
        setStatus("error");
        return;
      }
    }
    setStatus(result ? "success" : "notfound");
  };

  return (
    <Page title="Login">
      <div className="mx-auto text-center mb-10">
        <RedirectErrorMessage />
      </div>

      <div className="sm:mx-auto sm:w-full sm:max-w-md text-center">
        <Logo className="mx-auto h-12 w-auto text-primary" />
        <h2 className="text-center text-3xl text-foreground font-bold">Sign in to your account</h2>
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
                <div className="absolute inset-0 flex items-center" aria-hidden="true">
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
              <StatusMessage status={status} error={emailError} />
            </p>
          </form>
        </div>
        <LegalNotice operation="signin" />
        <RegionSwitch />
      </div>
    </Page>
  );
}
