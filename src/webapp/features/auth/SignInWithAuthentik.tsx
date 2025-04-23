import { Button } from "@components/Button";

export function SignInWithAuthentik() {
  const url = `/api/_auth/authentik`;

  return (
    <Button asChild variant="outline">
      <a href={url} className="flex space-x-2 w-full">
        <span>Continue with OIDC</span>
      </a>
    </Button>
  );
}
