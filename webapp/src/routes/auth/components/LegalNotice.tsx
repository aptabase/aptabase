import { isManagedCloud } from "@app/env";

type Props = {
  operation: "signin" | "signup";
};

export function LegalNotice(props: Props) {
  if (!isManagedCloud) return null;

  return (
    <p className="text-center mt-2 text-xs tracking-normal text-subtle">
      By {props.operation === "signin" ? "signing in" : "signing up"}, you agree
      to our{" "}
      <a
        href="https://aptabase.com/legal/terms"
        target="_blank"
        className="text-primary hover:underline"
      >
        Terms of Service
      </a>{" "}
      and{" "}
      <a
        href="https://aptabase.com/legal/privacy"
        target="_blank"
        className="text-primary hover:underline"
      >
        Privacy Policy
      </a>
      .
    </p>
  );
}
