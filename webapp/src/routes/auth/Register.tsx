import { requestRegisterLink } from "@app/auth";
import { Button, Head, Logo, TextInput } from "@app/primitives";
import { useState } from "react";
import { Link } from "react-router-dom";
import { DataResidency } from "./components/DataResidency";
import { LegalNotice } from "./components/LegalNotice";
import { RegionSwitch } from "./components/RegionSwitch";
import { SignInWithGitHub } from "./components/SignInWithGitHub";
import { SignInWithGoogle } from "./components/SignInWithGoogle";
import { isOAuthEnabled } from "@app/env";

type FormStatus = "idle" | "loading" | "success";

type StatusMessageProps = {
  status: FormStatus;
};

const StatusMessage = (props: StatusMessageProps) => {
  if (props.status === "success") {
    return (
      <span className="text-success">
        Woo-hoo! Email sent, go check your inbox!
      </span>
    );
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

Component.displayName = "Register";
export function Component() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [status, setStatus] = useState<FormStatus>("idle");

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setStatus("loading");

    await requestRegisterLink(name, email);

    setStatus("success");
  };

  return (
    <>
      <Head title="Sign up" />
      <div className="sm:mx-auto sm:w-full sm:max-w-md">
        <Logo className="mx-auto h-20 w-auto text-primary" />
        <h2 className="text-center text-3xl font-bold">Get started for free</h2>
        <DataResidency />
      </div>
      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="py-8 px-4 sm:rounded-lg sm:px-10">
          {isOAuthEnabled && (
            <div className="space-y-2">
              <SignInWithGitHub />
              <SignInWithGoogle />
            </div>
          )}

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
        </div>
        <LegalNotice operation="signup" />
        <RegionSwitch />
      </div>
    </>
  );
}
